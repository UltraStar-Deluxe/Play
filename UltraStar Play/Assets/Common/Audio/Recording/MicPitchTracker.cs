using System;
using System.Collections.Generic;
using UniInject;
using UniRx;
using UnityEngine;
using static MicSampleRecorder;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

// Analyzes the newest samples from a mic and fires an event for the analysis result.
[RequireComponent(typeof(MicSampleRecorder))]
public class MicPitchTracker : MonoBehaviour, INeedInjection
{
    // Longest period of singable notes (C2) requires 674 samples at 44100 Hz sample rate.
    // Thus, 1024 samples should be sufficient.
    private const int MaxSampleCountToUse = 2048;

    // Wait until at least this amout of new samples is available in the mic buffer.
    // This makes the MicPitchTracker frame-rate independent.
    private const int MinSampleCountToUse = 1024;
    private int bufferedNotAnalyzedSampleCount;

    [Range(0, 1)]
    public float halftoneContinuationBias = 0.1f;

    [ReadOnly]
    public string lastMidiNoteName;
    [ReadOnly]
    public int lastMidiNote;

    [Inject]
    private Settings settings;

    public MicProfile MicProfile
    {
        get
        {
            return MicSampleRecorder.MicProfile;
        }
        set
        {
            MicSampleRecorder.MicProfile = value;
            // The sample rate could have changed, which means a new analyzer is needed.
            audioSamplesAnalyzer = CreateAudioSamplesAnalyzer(settings.AudioSettings.pitchDetectionAlgorithm, MicSampleRecorder.SampleRateHz);
            audioSamplesAnalyzer.Enable();
        }
    }

    [Inject(searchMethod = SearchMethods.GetComponent)]
    public MicSampleRecorder MicSampleRecorder { get; private set; }

    private readonly Subject<PitchEvent> pitchEventStream = new Subject<PitchEvent>();
    public IObservable<PitchEvent> PitchEventStream
    {
        get
        {
            return pitchEventStream;
        }
    }

    private IAudioSamplesAnalyzer audioSamplesAnalyzer;

    void Start()
    {
        // Update label in inspector for debugging.
        pitchEventStream.Subscribe(UpdateLastMidiNoteFields);
        MicSampleRecorder.RecordingEventStream.Subscribe(OnRecordingEvent);

        audioSamplesAnalyzer = CreateAudioSamplesAnalyzer(settings.AudioSettings.pitchDetectionAlgorithm, MicSampleRecorder.SampleRateHz);
        settings.AudioSettings.ObserveEveryValueChanged(it => it.pitchDetectionAlgorithm)
            .Subscribe(OnPitchDetectionAlgorithmChanged);
    }

    void Update()
    {
        if (audioSamplesAnalyzer is CamdAudioSamplesAnalyzer)
        {
            (audioSamplesAnalyzer as CamdAudioSamplesAnalyzer).HalftoneContinuationBias = halftoneContinuationBias;
        }
    }

    public static IAudioSamplesAnalyzer CreateAudioSamplesAnalyzer(EPitchDetectionAlgorithm pitchDetectionAlgorithm, int sampleRateHz)
    {
        switch (pitchDetectionAlgorithm)
        {
            case EPitchDetectionAlgorithm.Camd:
                CamdAudioSamplesAnalyzer camdAudioSamplesAnalyzer = new CamdAudioSamplesAnalyzer(sampleRateHz, MaxSampleCountToUse);
                return camdAudioSamplesAnalyzer;
            case EPitchDetectionAlgorithm.Dywa:
                DywaAudioSamplesAnalyzer dywaAudioSamplesAnalyzer = new DywaAudioSamplesAnalyzer(sampleRateHz, MaxSampleCountToUse);
                return dywaAudioSamplesAnalyzer;
            default:
                throw new UnityException("Unkown pitch detection algorithm:" + pitchDetectionAlgorithm);
        }
    }

    private void OnRecordingEvent(RecordingEvent recordingEvent)
    {
        // Detect the pitch of the sample
        int newSampleLength = recordingEvent.NewSamplesEndIndex - recordingEvent.NewSamplesStartIndex;
        bufferedNotAnalyzedSampleCount += newSampleLength;

        // Wait until enough new samples are buffered
        if (bufferedNotAnalyzedSampleCount < MinSampleCountToUse)
        {
            return;
        }

        // Do not analyze more than necessary
        if (bufferedNotAnalyzedSampleCount > MaxSampleCountToUse)
        {
            bufferedNotAnalyzedSampleCount = MaxSampleCountToUse;
        }

        // Analyze the newest portion of the not-yet-analyzed MicSamples
        int startIndex = recordingEvent.MicSamples.Length - bufferedNotAnalyzedSampleCount;
        int endIndex = recordingEvent.MicSamples.Length;
        PitchEvent pitchEvent = audioSamplesAnalyzer.ProcessAudioSamples(recordingEvent.MicSamples, startIndex, endIndex, MicProfile);
        bufferedNotAnalyzedSampleCount = 0;

        // Notify listeners
        pitchEventStream.OnNext(pitchEvent);
    }

    private void UpdateLastMidiNoteFields(PitchEvent pitchEvent)
    {
        if (pitchEvent == null)
        {
            lastMidiNoteName = "";
            lastMidiNote = 0;
        }
        else
        {
            lastMidiNoteName = MidiUtils.GetAbsoluteName(pitchEvent.MidiNote);
            lastMidiNote = pitchEvent.MidiNote;
        }
    }

    private void OnPitchDetectionAlgorithmChanged(EPitchDetectionAlgorithm newValue)
    {
        if (MicProfile != null)
        {
            audioSamplesAnalyzer = CreateAudioSamplesAnalyzer(newValue, MicSampleRecorder.SampleRateHz);
            audioSamplesAnalyzer.Enable();
        }
    }
}
