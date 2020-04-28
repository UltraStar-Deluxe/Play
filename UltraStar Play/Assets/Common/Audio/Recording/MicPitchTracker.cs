using System;
using System.Collections.Generic;
using UniInject;
using UniRx;
using UnityEngine;
using static MicSampleRecorder;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

[RequireComponent(typeof(MicSampleRecorder))]
public class MicPitchTracker : MonoBehaviour, INeedInjection
{
    // Longest period of singable notes (C2) requires 674 samples at 44100 Hz sample rate.
    // Thus, 1024 samples should be sufficient.
    private const int MaxSampleCountToUse = 2048;

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
            audioSamplesAnalyzer = CreateAudioSamplesAnalyzer(settings.AudioSettings.pitchDetectionAlgorithm);
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
        MicSampleRecorder.RecordingEventStream.Subscribe(AnalyzePitchOfRecordingEvent);

        audioSamplesAnalyzer = CreateAudioSamplesAnalyzer(settings.AudioSettings.pitchDetectionAlgorithm);
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

    private IAudioSamplesAnalyzer CreateAudioSamplesAnalyzer(EPitchDetectionAlgorithm pitchDetectionAlgorithm)
    {
        switch (pitchDetectionAlgorithm)
        {
            case EPitchDetectionAlgorithm.Camd:
                CamdAudioSamplesAnalyzer camdAudioSamplesAnalyzer = new CamdAudioSamplesAnalyzer(MicSampleRecorder.SampleRateHz, MaxSampleCountToUse);
                camdAudioSamplesAnalyzer.HalftoneContinuationBias = halftoneContinuationBias;
                return camdAudioSamplesAnalyzer;
            case EPitchDetectionAlgorithm.Dywa:
                DywaAudioSamplesAnalyzer dywaAudioSamplesAnalyzer = new DywaAudioSamplesAnalyzer(MicSampleRecorder.SampleRateHz, MaxSampleCountToUse);
                return dywaAudioSamplesAnalyzer;
            default:
                throw new UnityException("Unkown pitch detection algorithm:" + SettingsManager.Instance.Settings.AudioSettings.pitchDetectionAlgorithm);
        }
    }

    private void AnalyzePitchOfRecordingEvent(RecordingEvent recordingEvent)
    {
        // Detect the pitch of the sample
        int sampleLength = recordingEvent.NewSamplesEndIndex - recordingEvent.NewSamplesStartIndex;
        int endIndex = (sampleLength > MaxSampleCountToUse)
            ? recordingEvent.NewSamplesStartIndex + MaxSampleCountToUse
            : recordingEvent.NewSamplesEndIndex;
        PitchEvent pitchEvent = audioSamplesAnalyzer.ProcessAudioSamples(recordingEvent.MicSamples, recordingEvent.NewSamplesStartIndex, endIndex, MicProfile);

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
            audioSamplesAnalyzer = CreateAudioSamplesAnalyzer(newValue);
            audioSamplesAnalyzer.Enable();
        }
    }
}
