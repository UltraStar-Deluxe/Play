using System;
using System.Collections.Generic;
using System.Linq;
using PortAudioForUnity;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MicSampleRecorderManager : AbstractSingletonBehaviour, INeedInjection
{
    public static MicSampleRecorderManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<MicSampleRecorderManager>();

    [Inject]
    private ISettings settings;

    private readonly List<MicSampleRecorder> micSampleRecorders = new();
    public IReadOnlyList<MicSampleRecorder> MicSampleRecorders => micSampleRecorders;

    private readonly Subject<ConnectedMicDevicesChangedEvent> connectedMicDevicesChangesStream = new();
    public IObservable<ConnectedMicDevicesChangedEvent> ConnectedMicDevicesChangesStream => connectedMicDevicesChangesStream;

    private string[] CurrentConnectedMicDevices => Microphone.devices;
    private string[] lastConnectedMicDevices;

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
                float outputAmplificationFactor = NumberUtils.PercentToFactor(newValue);
                foreach (MicSampleRecorder it in micSampleRecorders)
                {
                    it.OutputVolume = outputAmplificationFactor;
                }

                foreach (DeviceInfo deviceInfo in PortAudioUtils.DeviceInfos)
                {
                    PortAudioUtils.SetOutputAmplificationFactor(deviceInfo, outputAmplificationFactor);
                }
            });

        settings.ObserveEveryValueChanged(it => it.PortAudioHostApi)
            .Subscribe(newValue =>
            {
                MicrophoneAdapter.SetHostApi(PortAudioConversionUtils.ConvertHostApi(newValue));
            });

        settings.ObserveEveryValueChanged(it => it.PortAudioOutputDeviceName)
            .Subscribe(newValue =>
            {
                micSampleRecorders.ForEach(it => it.PortAudioOutputDeviceName = newValue);
            });

        Debug.Log($"Initial connected mic devices: {JsonConverter.ToJson(CurrentConnectedMicDevices)}");
        SetLastConnectedMicDevices(CurrentConnectedMicDevices);
    }

    private void Update()
    {
        UpdateConnectedMicDevices();
    }

    public MicSampleRecorder GetOrCreateMicSampleRecorder(MicProfile micProfile)
    {
        if (micProfile == null
            || GameObjectUtils.IsDestroyed(this))
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
        micSampleRecorder.PortAudioOutputDeviceName = settings.PortAudioOutputDeviceName;
        micSampleRecorder.OutputVolume = NumberUtils.PercentToFactor(settings.MicrophonePlaybackVolumePercent);
        micSampleRecorders.Add(micSampleRecorder);
        return micSampleRecorder;
    }

    private void SetLastConnectedMicDevices(string[] devices)
    {
        // Create copy of array
        lastConnectedMicDevices = devices.ToArray();
    }

    private void UpdateConnectedMicDevices()
    {
        if (CurrentConnectedMicDevices == null
            || lastConnectedMicDevices == null
            || CurrentConnectedMicDevices.SequenceEqual(lastConnectedMicDevices))
        {
            return;
        }

        Debug.Log($"Connected mic devices changed, new: {JsonConverter.ToJson(CurrentConnectedMicDevices)}, old: {JsonConverter.ToJson(lastConnectedMicDevices)}");
        ConnectedMicDevicesChangedEvent evt = new(CurrentConnectedMicDevices, lastConnectedMicDevices);
        SetLastConnectedMicDevices(CurrentConnectedMicDevices);
        connectedMicDevicesChangesStream.OnNext(evt);
    }
}
