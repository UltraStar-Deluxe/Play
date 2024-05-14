using System;
using System.Collections.Generic;
using System.Linq;
using CircularBuffer;
using UniInject;
using UniRx;
using UnityEngine;

public class MicProgressBarRecordingControl : INeedInjection, IInjectionFinishedListener, IDisposable
{
    private const float TargetNoiseAboveThresholdDurationInMillis = 1000;
    
    public MicProgressBarControl MicProgressBarControl { get; private set; } = new();

    [Inject]
    private Injector injector;
    
    [Inject]
    private MicSampleRecorderManager micSampleRecorderManager;
    
    [Inject(Optional = true)]
    private IServerSideCompanionClientManager serverSideCompanionClientManager;
    
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

    private readonly CircularBuffer<int> lastReceivedMidiNotesFromCompanionClient = new(10);
    
    public void OnInjectionFinished()
    {
        injector.Inject(MicProgressBarControl);
        UpdateRecordingEventSubscription();
    }

    public void Update()
    {
        long currentTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        long timeSinceLastRecordingEvent = currentTimeInMillis - lastRecordingEventTimeInMillis;
        if (timeSinceLastRecordingEvent > 500)
        {
            noiseAboveThresholdDurationInMillis -= Time.deltaTime * 1000;
        }
        noiseAboveThresholdDurationInMillis = NumberUtils.Limit(noiseAboveThresholdDurationInMillis, 0, TargetNoiseAboveThresholdDurationInMillis);

        UpdateProgressBarValue();
    }

    private void UpdateRecordingEventSubscription()
    {
        micSampleRecorderDisposables.ForEach(d => d.Dispose());
        micSampleRecorderDisposables.Clear();
        
        MicSampleRecorder micSampleRecorder = micSampleRecorderManager.GetOrCreateMicSampleRecorder(MicProfile);
        if (micSampleRecorder != null)
        {
            // Subscribe to recording events
            micSampleRecorderDisposables.Add(micSampleRecorder.RecordingEventStream.Subscribe(evt => OnRecordingEvent(evt)));
            micSampleRecorderDisposables.Add(micSampleRecorder.IsRecording
                .Subscribe(isRecording =>
                {
                    if (!isRecording)
                    {
                        MicProgressBarControl.ProgressBarValue = 0;
                    }
                }));
            
            // Subscribe to Companion App messages
            MicProfile micProfile = micSampleRecorder.MicProfile;
            if (micProfile != null
                && micProfile.IsInputFromConnectedClient
                && serverSideCompanionClientManager != null)
            {
                if (serverSideCompanionClientManager.TryGet(micProfile.ConnectedClientId,
                        out ICompanionClientHandler companionClientHandler))
                {
                    micSampleRecorderDisposables.Add(companionClientHandler.ReceivedMessageStream
                        .Subscribe(evt => OnCompanionClientMessageReceived(evt)));
                }
            }
        }
        
        lastRecordingEventTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
    }

    private void OnCompanionClientMessageReceived(JsonSerializable evt)
    {
        if (evt is StopRecordingMessageDto)
        {
            MicProgressBarControl.ProgressBarValue = 0;
        }
        else if (evt is BeatPitchEventsDto beatPitchEventsDto
                 && !beatPitchEventsDto.BeatPitchEvents.IsNullOrEmpty())
        {
            // Increase the progress bar when singing the same note for a longer time
            long currentTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
            long deltaTimeInMillis = Math.Abs(currentTimeInMillis - lastRecordingEventTimeInMillis) < 500
                ? currentTimeInMillis - lastRecordingEventTimeInMillis
                : 0;
            
            BeatPitchEventDto beatPitchEventDto = beatPitchEventsDto.BeatPitchEvents.LastOrDefault();
            int midiNote = beatPitchEventDto.MidiNote;
            if (midiNote <= 0)
            {
                return;
            }
            
            lastReceivedMidiNotesFromCompanionClient.PushBack(midiNote);
            int medianMidiNote = NumberUtils.Median(lastReceivedMidiNotesFromCompanionClient.ToList());
            if (Math.Abs(medianMidiNote - beatPitchEventDto.MidiNote) <= 2)
            {
                noiseAboveThresholdDurationInMillis += deltaTimeInMillis;
            }
            else
            {
                noiseAboveThresholdDurationInMillis -= deltaTimeInMillis;
            }

            lastRecordingEventTimeInMillis = currentTimeInMillis;
        }
    }

    private void OnRecordingEvent(RecordingEvent evt)
    {
        int noiseSuppression = MicProfile?.NoiseSuppression ?? 0;
        noiseSuppression = NumberUtils.Limit(noiseSuppression, 10, 100);
        bool isAboveThreshold = AbstractAudioSamplesAnalyzer.IsAboveNoiseSuppressionThreshold(evt.MicSamples, evt.NewSamplesStartIndex, evt.NewSamplesEndIndex, noiseSuppression);

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
    }

    private void UpdateProgressBarValue()
    {
        if (MicProfile != null)
        {
            MicProgressBarControl.ProgressBarValue = (float)(100 * (noiseAboveThresholdDurationInMillis / TargetNoiseAboveThresholdDurationInMillis));
        }
        else
        {
            MicProgressBarControl.ProgressBarValue = 0;
        }
    }
    
    public void Dispose()
    {
        micSampleRecorderDisposables.ForEach(d => d.Dispose());
        micSampleRecorderDisposables.Clear();
    }
}
