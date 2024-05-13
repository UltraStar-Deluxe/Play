
using System;
using System.Collections.Generic;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public abstract class AbstractMicPitchTracker : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    // Longest period of singable notes (C2) requires 674 samples at 44100 Hz sample rate.
    // Thus, 1024 samples should be sufficient.
    protected const int MaxSampleCountToUse = 2048;

    [Range(0, 1)]
    public float halftoneContinuationBias = 0.1f;

    [Inject]
    protected ISettings settings;

    [Inject]
    protected MicSampleRecorderManager micSampleRecorderManager;

    protected MicProfile micProfile;
    public MicProfile MicProfile
    {
        get => micProfile;
        set
        {
            DisposeMicSampleRecorderDisposables();

            micProfile = value;

            if (MicSampleRecorder == null)
            {
                return;
            }

            // Listen to changes
            micSampleRecorderDisposables.Add(MicSampleRecorder.FinalSampleRate.Subscribe(newValue => FinalSampleRate.Value = newValue));
            micSampleRecorderDisposables.Add(MicSampleRecorder.IsRecording.Subscribe(newValue => IsRecording.Value = newValue));
            micSampleRecorderDisposables.Add(MicSampleRecorder.RecordingEventStream.Subscribe(evt => recordingEventStream.OnNext(evt)));

            // The sample rate could have changed, which means a new analyzer is needed.
            AudioSamplesAnalyzer = CreateAudioSamplesAnalyzer(settings.PitchDetectionAlgorithm, MicSampleRecorder.FinalSampleRate.Value);
        }
    }

    public float[] MicSamples
    {
        get
        {
            if (MicSampleRecorder == null)
            {
                return Array.Empty<float>();
            }

            return MicSampleRecorder.MicSamples;
        }
    }

    public ReactiveProperty<int> FinalSampleRate { get; private set; } = new(MicSampleRecorder.DefaultSampleRate);
    public ReactiveProperty<bool> IsRecording { get; private set; } = new();

    private readonly Subject<RecordingEvent> recordingEventStream = new();
    public IObservable<RecordingEvent> RecordingEventStream => recordingEventStream;

    public bool PlayRecordedAudio
    {
        get
        {
            if (MicSampleRecorder == null)
            {
                return false;
            }

            return MicSampleRecorder.PlayRecordedAudio;
        }

        set
        {
            if (MicSampleRecorder == null)
            {
                return;
            }

            MicSampleRecorder.PlayRecordedAudio = value;
        }
    }

    protected virtual MicSampleRecorder MicSampleRecorder => micSampleRecorderManager.GetOrCreateMicSampleRecorder(micProfile);

    protected readonly Subject<PitchEvent> pitchEventStream = new();
    public IObservable<PitchEvent> PitchEventStream => pitchEventStream;

    public IAudioSamplesAnalyzer AudioSamplesAnalyzer { get; protected set; }

    protected readonly List<IDisposable> micSampleRecorderDisposables = new();

    public virtual void OnInjectionFinished()
    {
        settings.ObserveEveryValueChanged(it => it.PitchDetectionAlgorithm)
            .Subscribe(OnPitchDetectionAlgorithmChanged)
            .AddTo(gameObject);
    }

    protected virtual void Update()
    {
        if (AudioSamplesAnalyzer is CamdAudioSamplesAnalyzer camdAudioSamplesAnalyzer)
        {
            camdAudioSamplesAnalyzer.HalftoneContinuationBias = halftoneContinuationBias;
        }
    }

    private void OnPitchDetectionAlgorithmChanged(EPitchDetectionAlgorithm newValue)
    {
        if (MicProfile == null
            || MicSampleRecorder == null)
        {
            return;
        }

        AudioSamplesAnalyzer = CreateAudioSamplesAnalyzer(newValue, MicSampleRecorder.FinalSampleRate.Value);
    }

    public virtual void StartRecording()
    {
        if (MicSampleRecorder == null)
        {
            return;
        }

        MicSampleRecorder.StartRecording();
    }

    public virtual void StopRecording()
    {
        if (MicSampleRecorder == null)
        {
            return;
        }

        MicSampleRecorder.StopRecording();
    }

    public static PitchEvent AnalyzeBeat(
        SongMeta songMeta,
        int beat,
        double positionInMillis,
        int micSampleRate,
        int micDelayInMillis,
        int micAmplificationFactor,
        int micNoiseSuppression,
        float[] micSampleBuffer,
        IAudioSamplesAnalyzer audioSamplesAnalyzer)
    {
        if (micSampleRate <= 0)
        {
            Debug.LogWarning("Sample rate should be a positive value");
            return null;
        }

        float beatStartInMillis = (float)SongMetaBpmUtils.BeatsToMillis(songMeta, beat);
        float beatEndInMillis = (float)SongMetaBpmUtils.BeatsToMillis(songMeta, beat + 1);
        float beatLengthInMillis = beatEndInMillis - beatStartInMillis;
        int beatLengthInSamples = (int)(beatLengthInMillis * micSampleRate / 1000f);

        // The newest sample in the buffer corresponds to (position - micDelay)
        float positionInMillisConsideringMicDelay = (float)(positionInMillis - micDelayInMillis);
        float distanceToNewestSamplesInMillis = positionInMillisConsideringMicDelay - beatEndInMillis;
        int distanceToNewestSamplesInSamples = (int)(distanceToNewestSamplesInMillis * micDelayInMillis / 1000f);
        distanceToNewestSamplesInSamples = NumberUtils.Limit(distanceToNewestSamplesInSamples, 0, micSampleBuffer.Length - 1);

        int endIndex = micSampleBuffer.Length - distanceToNewestSamplesInSamples;
        int startIndex = endIndex - beatLengthInSamples;
        endIndex = NumberUtils.Limit(endIndex, 0, micSampleBuffer.Length - 1);
        startIndex = NumberUtils.Limit(startIndex, 0, micSampleBuffer.Length - 1);
        if (endIndex < startIndex)
        {
            Debug.LogWarning($"Cannot analyze from sample {startIndex} to {endIndex}. Start index must be smaller than end index.");
            return null;
        }

        PitchEvent pitchEvent = audioSamplesAnalyzer.ProcessAudioSamples(micSampleBuffer, startIndex, endIndex, micAmplificationFactor, micNoiseSuppression);
        return pitchEvent;
    }

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

    protected virtual void OnDestroy()
    {
        DisposeMicSampleRecorderDisposables();
    }

    protected void DisposeMicSampleRecorderDisposables()
    {
        micSampleRecorderDisposables.ForEach(it => it.Dispose());
        micSampleRecorderDisposables.Clear();
    }
}
