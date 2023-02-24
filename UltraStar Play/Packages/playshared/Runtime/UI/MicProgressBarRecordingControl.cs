using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;

public class MicProgressBarRecordingControl : INeedInjection, IInjectionFinishedListener
{
    private const float SampleVolumeThreshold = 0.3f;
    private const float TargetNoiseAboveThresholdDurationInMillis = 1000;
    
    public MicProgressBarControl MicProgressBarControl { get; private set; } = new();

    [Inject]
    private Injector injector;

    [Inject]
    private MicSampleRecorder micSampleRecorder;

    private MicProfile MicProfile => MicProgressBarControl.MicProfile;

    private long lastRecordingEventTimeInMillis;
    private double noiseAboveThresholdDurationInMillis;
    
    public void OnInjectionFinished()
    {
        injector.Inject(MicProgressBarControl);

        if (micSampleRecorder != null)
        {
            micSampleRecorder.RecordingEventStream.Subscribe(evt => OnRecordingEvent(evt));
        }
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
}
