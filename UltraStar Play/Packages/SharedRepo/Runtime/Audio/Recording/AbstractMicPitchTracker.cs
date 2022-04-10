
using System;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

[RequireComponent(typeof(MicSampleRecorder))]
public abstract class AbstractMicPitchTracker : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    // Longest period of singable notes (C2) requires 674 samples at 44100 Hz sample rate.
    // Thus, 1024 samples should be sufficient.
    protected const int MaxSampleCountToUse = 2048;

    [Range(0, 1)]
    public float halftoneContinuationBias = 0.1f;

    [Inject]
    protected ISettings settings;

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
            audioSamplesAnalyzer = CreateAudioSamplesAnalyzer(settings.PitchDetectionAlgorithm, MicSampleRecorder.SampleRateHz);
            audioSamplesAnalyzer.Enable();
        }
    }

    [Inject(SearchMethod = SearchMethods.GetComponent)]
    public MicSampleRecorder MicSampleRecorder { get; protected set; }

    protected readonly Subject<PitchEvent> pitchEventStream = new();
    public IObservable<PitchEvent> PitchEventStream => pitchEventStream;

    protected IAudioSamplesAnalyzer audioSamplesAnalyzer;

    public virtual void OnInjectionFinished()
    {
        MicSampleRecorder.RecordingEventStream.Subscribe(recordingEvent => OnRecordingEvent(recordingEvent));

        audioSamplesAnalyzer = CreateAudioSamplesAnalyzer(settings.PitchDetectionAlgorithm, MicSampleRecorder.SampleRateHz);
        settings.ObserveEveryValueChanged(it => it.PitchDetectionAlgorithm)
            .Subscribe(OnPitchDetectionAlgorithmChanged)
            .AddTo(gameObject);
    }

    protected virtual void Update()
    {
        if (audioSamplesAnalyzer is CamdAudioSamplesAnalyzer)
        {
            (audioSamplesAnalyzer as CamdAudioSamplesAnalyzer).HalftoneContinuationBias = halftoneContinuationBias;
        }
    }

    private void OnPitchDetectionAlgorithmChanged(EPitchDetectionAlgorithm newValue)
    {
        if (MicProfile == null)
        {
            return;
        }

        audioSamplesAnalyzer = CreateAudioSamplesAnalyzer(newValue, MicSampleRecorder.SampleRateHz);
        audioSamplesAnalyzer.Enable();
    }

    protected abstract void OnRecordingEvent(RecordingEvent recordingEvent);

    public static IAudioSamplesAnalyzer CreateAudioSamplesAnalyzer(EPitchDetectionAlgorithm pitchDetectionAlgorithm, int sampleRateHz)
    {
        switch (pitchDetectionAlgorithm)
        {
            case EPitchDetectionAlgorithm.Camd:
                CamdAudioSamplesAnalyzer camdAudioSamplesAnalyzer = new(sampleRateHz, MaxSampleCountToUse);
                return camdAudioSamplesAnalyzer;
            case EPitchDetectionAlgorithm.Dywa:
                DywaAudioSamplesAnalyzer dywaAudioSamplesAnalyzer = new(sampleRateHz, MaxSampleCountToUse);
                return dywaAudioSamplesAnalyzer;
            default:
                throw new UnityException("Unknown pitch detection algorithm:" + pitchDetectionAlgorithm);
        }
    }
}
