using System;
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

    private IDisposable recordingEventDisposable;

    private long lastRecordingEventTimeInMillis;
    private double noiseAboveThresholdDurationInMillis;
    
    public void OnInjectionFinished()
    {
        injector.Inject(MicProgressBarControl);
        UpdateRecordingEventSubscription();
    }

    private void UpdateRecordingEventSubscription()
    {
        if (recordingEventDisposable != null)
        {
            recordingEventDisposable.Dispose();
        }
        
        MicSampleRecorder micSampleRecorder = GameObject.FindObjectsOfType<MicSampleRecorder>()
            .FirstOrDefault(it => it.MicProfile == MicProfile);
        if (micSampleRecorder != null)
        {
            recordingEventDisposable = micSampleRecorder.RecordingEventStream
                .Subscribe(evt => OnRecordingEvent(evt));
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
        recordingEventDisposable?.Dispose();
    }
}
