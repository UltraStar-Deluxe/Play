using System;
using System.Linq;
using PortAudioForUnity;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

/**
 * Low-latency audio output via PortAudio.
 */
public class PortAudioPlaybackManager : AbstractSingletonBehaviour, INeedInjection
{
    public static PortAudioPlaybackManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<PortAudioPlaybackManager>();

    private const int BufferLengthInSeconds = 1;

    [Inject]
    private Settings settings;

    [Inject]
    private SceneNavigator sceneNavigator;

    public bool IsPlaying { get; private set; }

    public AudioClip AudioClip { get; private set; }
    private float[] audioClipAllSamples;
    private int audioClipChannelCount;
    private int audioClipSampleRate;
    private int audioClipLengthInMonoSamples;
    private int outputSampleIndex;

    private DeviceInfo outputDeviceInfo;

    public int PositionInMonoSamples
    {
        get => outputSampleIndex / audioClipChannelCount;
        set => outputSampleIndex = value * audioClipChannelCount;
    }

    public double PositionInSeconds
    {
        get => (double)PositionInMonoSamples / audioClipSampleRate;
        set => PositionInMonoSamples = (int)(value * audioClipSampleRate);
    }

    public int LengthInMonoSamples => audioClipLengthInMonoSamples;
    public double LengthInSeconds => audioClipLengthInMonoSamples / audioClipSampleRate;

    public bool IsLoaded => AudioClip != null && audioClipAllSamples != null;

    protected override object GetInstance()
    {
        return Instance;
    }

    private void Start()
    {
        settings.ObserveEveryValueChanged(it => it.PortAudioHostApi)
            .Subscribe(_ => UpdateOutputDeviceInfo())
            .AddTo(gameObject);

        settings.ObserveEveryValueChanged(it => it.PortAudioOutputDeviceName)
            .Subscribe(_ => UpdateOutputDeviceInfo())
            .AddTo(gameObject);

        sceneNavigator.BeforeSceneChangeEventStream
            .Subscribe(_ => StopPlayback())
            .AddTo(gameObject);
    }

    private void UpdateOutputDeviceInfo()
    {
        if (outputDeviceInfo != null)
        {
            PortAudioUtils.StopPlayback(outputDeviceInfo);
        }

        if (!SettingsUtils.ShouldUsePortAudio(settings))
        {
            return;
        }

        outputDeviceInfo = PortAudioUtils.DeviceInfos
            .FirstOrDefault(deviceInfo => deviceInfo.HostApi == PortAudioConversionUtils.ConvertHostApi(settings.PortAudioHostApi)
                                          && settings.PortAudioOutputDeviceName.IsNullOrEmpty() || deviceInfo.Name == settings.PortAudioOutputDeviceName
                                          && deviceInfo.MaxOutputChannels > 0);
    }

    private void OnReadAudioSamples(float[] data)
    {
        if (!IsPlaying
            || audioClipAllSamples == null)
        {
            Array.Clear(data, 0, data.Length);
            return;
        }

        for (int i = 0; i < data.Length && outputSampleIndex < audioClipAllSamples.Length; i++)
        {
            data[i] = audioClipAllSamples[outputSampleIndex];
            outputSampleIndex++;
        }
    }

    public void LoadAudioClip(AudioClip theAudioClip)
    {
        UpdateOutputDeviceInfo();
        if (outputDeviceInfo == null)
        {
            Debug.LogError("Could not determine PortAudio output device");
            return;
        }

        AudioClip = theAudioClip;
        audioClipChannelCount = AudioClip.channels;
        audioClipSampleRate = AudioClip.frequency;
        audioClipLengthInMonoSamples = AudioClip.samples;

        audioClipAllSamples = new float[audioClipLengthInMonoSamples * audioClipChannelCount];
        AudioClip.GetData(audioClipAllSamples, 0);

        outputSampleIndex = 0;
    }

    public void PausePlayback()
    {
        IsPlaying = false;
    }

    public void StartPlayback()
    {
        if (IsPlaying)
        {
            return;
        }

        if (AudioClip == null)
        {
            Debug.LogError("Cannot start playback, no AudioClip");
            return;
        }

        IsPlaying = true;

        UpdateOutputDeviceInfo();
        if (outputDeviceInfo == null)
        {
            Debug.LogError("Could not determine PortAudio output device");
            return;
        }

        PortAudioUtils.StartPlayback(
            outputDeviceInfo,
            audioClipChannelCount,
            BufferLengthInSeconds,
            audioClipSampleRate,
            OnReadAudioSamples);
    }

    public void StopPlayback()
    {
        if (!IsPlaying)
        {
            return;
        }

        IsPlaying = false;
        PortAudioUtils.StopPlayback(outputDeviceInfo);
        outputSampleIndex = 0;
    }

    public void Unload()
    {
        StopPlayback();

        AudioClip = null;
        audioClipChannelCount = 0;
        audioClipSampleRate = 0;
        audioClipLengthInMonoSamples = 0;
        outputSampleIndex = 0;
        audioClipAllSamples = null;
    }
}
