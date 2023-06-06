using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MicSampleRecorderManager : AbstractSingletonBehaviour, INeedInjection
{
    public static MicSampleRecorderManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<MicSampleRecorderManager>();

    [Inject]
    private ISettings settings;
    
    private readonly List<MicSampleRecorder> micSampleRecorders = new();
    public IReadOnlyList<MicSampleRecorder> MicSampleRecorders => micSampleRecorders;
    
    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void AwakeSingleton()
    {
        MicSampleRecorder[] micSampleRecordersInChildren = GetComponentsInChildren<MicSampleRecorder>();
        foreach (MicSampleRecorder micSampleRecorder in micSampleRecordersInChildren)
        {
            if (micSampleRecorder.MicProfile == null)
            {
                Destroy(micSampleRecorder);
            }
            else
            {
                micSampleRecorders.Add(micSampleRecorder);
            }
        }
    }

    protected override void StartSingleton()
    {
        settings.ObserveEveryValueChanged(it => it.PlayRecordedAudio)
            .Subscribe(newValue =>
            {
                micSampleRecorders.ForEach(it => it.PlayRecordedAudio = newValue);
            });
        
        settings.ObserveEveryValueChanged(it => it.MicrophonePlaybackVolumePercent)
            .Subscribe(newValue =>
            {
                micSampleRecorders.ForEach(it =>
                {
                    float finalVolume = NumberUtils.PercentToFactor(newValue);
                    it.Volume = finalVolume;
                });
            });
    }
    
    public MicSampleRecorder GetOrCreateMicSampleRecorder(MicProfile micProfile)
    {
        if (micProfile == null)
        {
            return null;
        }

        MicSampleRecorder micSampleRecorder = micSampleRecorders.FirstOrDefault(it =>
            it.MicProfile != null
            && it.MicProfile.Name == micProfile.Name
            && it.MicProfile.ChannelIndex == micProfile.ChannelIndex);
        if (micSampleRecorder != null)
        {
            return micSampleRecorder;
        }
        
        GameObject micSampleRecorderGameObject = new GameObject($"MicSampleRecorder '{micProfile.GetDisplayNameWithChannel()}'");
        micSampleRecorderGameObject.transform.parent = transform;
        micSampleRecorderGameObject.AddComponent<AudioSource>();
        micSampleRecorder = micSampleRecorderGameObject.AddComponent<MicSampleRecorder>();
        micSampleRecorder.MicProfile = micProfile;
        micSampleRecorder.PlayRecordedAudio = settings.PlayRecordedAudio;
        micSampleRecorders.Add(micSampleRecorder);
        return micSampleRecorder;
    }
}
