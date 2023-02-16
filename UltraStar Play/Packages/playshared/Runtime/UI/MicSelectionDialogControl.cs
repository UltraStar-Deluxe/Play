using System;
using System.Collections.Generic;
using System.Linq;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class MicSelectionDialogControl : MessageDialogControl, INeedInjection, IInjectionFinishedListener
{
    private const float SampleVolumeThreshold = 0.3f;
    private const float TargetNoiseAboveThresholdDurationInMillis = 1000;
    
    [Inject(Key = nameof(micWithNameUi))]
    private VisualTreeAsset micWithNameUi;

    private List<MicProfile> micProfiles;
    public List<MicProfile> MicProfiles
    {
        get => micProfiles;
        set
        {
            micProfiles = value;
            UpdateMicProfileList();
        }
    }

    public Action<MicProfile> OnMicProfileSelected { get; set; }

    private readonly List<MicWithNameControl> micWithNameControls = new();
    
    private List<MicSampleRecorder> micSampleRecorders;
    private readonly Dictionary<MicProfile, float> micProfileToNoiseAboveThresholdDurationInMillis = new();
    private readonly Dictionary<MicProfile, long> micProfileToLastRecordingEventTimeInMillis = new();

    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();
        AddButton(TranslationManager.GetTranslation("cancel"), () => CloseDialog());

        // Use mic sample recorders in scene to select mic by singing
        micSampleRecorders = GameObject.FindObjectsOfType<MicSampleRecorder>().ToList();
        if (!micSampleRecorders.IsNullOrEmpty())
        {
            micSampleRecorders.ForEach(micSampleRecorder =>
            {
                micSampleRecorder.RecordingEventStream.Subscribe(evt => OnRecordingEvent(micSampleRecorder, evt));
            });
        }
    }

    private void OnRecordingEvent(MicSampleRecorder micSampleRecorder, RecordingEvent evt)
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
        MicProfile micProfile = micSampleRecorder.MicProfile;
        if (!micProfileToNoiseAboveThresholdDurationInMillis.ContainsKey(micProfile))
        {
            micProfileToNoiseAboveThresholdDurationInMillis[micProfile] = 0;
            micProfileToLastRecordingEventTimeInMillis[micProfile] = 0;
        }

        long currentTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        long durationInMillis = currentTimeInMillis - micProfileToLastRecordingEventTimeInMillis[micProfile];
        micProfileToLastRecordingEventTimeInMillis[micProfile] = currentTimeInMillis;
        
        if (isAboveThreshold)
        {
            micProfileToNoiseAboveThresholdDurationInMillis[micProfile] += durationInMillis;
        }
        else
        {
            micProfileToNoiseAboveThresholdDurationInMillis[micProfile] -= durationInMillis;
        }
        micProfileToNoiseAboveThresholdDurationInMillis[micProfile] = NumberUtils.Limit(micProfileToNoiseAboveThresholdDurationInMillis[micProfile], 0, TargetNoiseAboveThresholdDurationInMillis);
        
        // Update progress in UI
        MicWithNameControl micWithNameControl = micWithNameControls.FirstOrDefault(it => it.MicProfile == micProfile);
        if (micWithNameControl != null)
        {
            micWithNameControl.ProgressBarValue = 100 * (micProfileToNoiseAboveThresholdDurationInMillis[micProfile] / TargetNoiseAboveThresholdDurationInMillis);
        }
    }

    private void UpdateMicProfileList()
    {
        dialogMessageContainer.Clear();
        micWithNameControls.Clear();
        
        micProfiles.ForEach(otherMicProfile =>
        {
            VisualElement micWithName = micWithNameUi.CloneTreeAndGetFirstChild();
            AddVisualElement(micWithName);
            
            MicWithNameControl micWithNameControl = injector
                .WithRootVisualElement(micWithName)
                .WithBindingForInstance(otherMicProfile)
                .CreateAndInject<MicWithNameControl>();
            micWithNameControl.OnMicSelected = OnMicSelected;
            
            micWithNameControls.Add(micWithNameControl);
        });
    }
    
    private void OnMicSelected(MicProfile newMicProfile)
    {
        OnMicProfileSelected?.Invoke(newMicProfile);
        CloseDialog();
    }
    
    public class MicProfileChangedEvent
    {
        public string playerName;
        public MicProfile oldMicProfile;
        public MicProfile newMicProfile;
    }
}
