using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;

public class MicProgressBarRecordingControl : INeedInjection, IInjectionFinishedListener, IDisposable
{
    private const float SampleVolumeThreshold = 0.3f;
    private const float TargetNoiseAboveThresholdDurationInMillis = 1000;
    
    public MicProgressBarControl MicProgressBarControl { get; private set; } = new();

    [Inject]
    private Injector injector;

    public MicProfile MicProfile
    {
        get => MicProgressBarControl.MicProfile;
        set
        {
            MicProgressBarControl.MicProfile = value;
            UpdateRecordingEventSubscription();
        }
    }

    private readonly List<IDisposable> micSampleRecorderDisposables = new();

    private long lastRecordingEventTimeInMillis;
    private double noiseAboveThresholdDurationInMillis;
    
    public void OnInjectionFinished()
    {
        injector.Inject(MicProgressBarControl);
        UpdateRecordingEventSubscription();
    }

    private void UpdateRecordingEventSubscription()
    {
        micSampleRecorderDisposables.ForEach(d => d.Dispose());
        micSampleRecorderDisposables.Clear();
        
        MicSampleRecorder micSampleRecorder = GameObject.FindObjectsOfType<MicSampleRecorder>()
            .FirstOrDefault(it => it.MicProfile == MicProfile);
        if (micSampleRecorder != null)
        {
            micSampleRecorderDisposables.Add(micSampleRecorder.RecordingEventStream
                .Subscribe(evt => OnRecordingEvent(evt)));
            micSampleRecorderDisposables.Add(micSampleRecorder.IsRecording
                .Subscribe(isRecording =>
                {
                    if (!isRecording)
                    {
                        MicProgressBarControl.ProgressBarValue = 0;
                    }
                }));
        }
        
        lastRecordingEventTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
    }
    
    private void OnRecordingEvent(RecordingEvent evt)
    {
        bool isAboveThreshold = false;
        for (int sampleIndex = evt.NewSamplesStartIndex; sampleIndex < evt.NewSamplesEndIndex; sampleIndex++)
        {
            float sample = evt.MicSamples[sampleIndex];
            if (Mathf.Abs(sample) > SampleVolumeThreshold)
            {
                isAboveThreshold = true;
                break;
            }
        }

        // Increase / decrease time above threshold
        long currentTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        long durationSinceLastRecordingEventInMillis = currentTimeInMillis - lastRecordingEventTimeInMillis;
        lastRecordingEventTimeInMillis = currentTimeInMillis;

        if (isAboveThreshold)
        {
            noiseAboveThresholdDurationInMillis += durationSinceLastRecordingEventInMillis;
        }
        else
        {
            noiseAboveThresholdDurationInMillis -= durationSinceLastRecordingEventInMillis;
        }
        noiseAboveThresholdDurationInMillis = NumberUtils.Limit(noiseAboveThresholdDurationInMillis, 0, TargetNoiseAboveThresholdDurationInMillis);
        
        // Update progress in UI
        MicProgressBarControl.ProgressBarValue = (float)(100 * (noiseAboveThresholdDurationInMillis / TargetNoiseAboveThresholdDurationInMillis));
    }

    public void Dispose()
    {
        micSampleRecorderDisposables.ForEach(d => d.Dispose());
        micSampleRecorderDisposables.Clear();
    }
}
