using System;
using PortAudioForUnity;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SineToneAudioSource : MonoBehaviour
{
    public bool SkipAudioOutput { get; set; }

    public bool usePortAudio;
    public bool UsePortAudio
    {
        get
        {
            return usePortAudio;
        }
        set
        {
            usePortAudio = value;

            // Update sample rate
            int newSampleRate = usePortAudio
                ? (int)OutputDeviceInfo.DefaultSampleRate
                : AudioSettings.outputSampleRate;
            sineToneGenerator = new(Frequency, newSampleRate);
        }
    }

    public PortAudioHostApi hostApi = PortAudioHostApi.Default;

    public int Frequency
    {
        get => sineToneGenerator.Frequency;
        set => sineToneGenerator.Frequency = value;
    }

    public int SampleRate
    {
        get => sineToneGenerator.SampleRate;
        set => sineToneGenerator.SampleRate = value;
    }

    private AudioSource audioSource;
    private SineToneGenerator sineToneGenerator;

    private HostApiInfo HostApiInfo => PortAudioUtils.GetHostApiInfo(PortAudioConversionUtils.ConvertHostApi(hostApi));
    private DeviceInfo OutputDeviceInfo => PortAudioUtils.GetDeviceInfo(HostApiInfo.DefaultOutputDeviceGlobalIndex);

    public bool Mute
    {
        get => audioSource.mute;
        set => audioSource.mute = value;
    }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        sineToneGenerator = new(440, AudioSettings.outputSampleRate);
    }

    public void Play()
    {
        if (UsePortAudio)
        {
            PortAudioUtils.StartPlayback(
                OutputDeviceInfo,
                OutputDeviceInfo.MaxOutputChannels,
                1,
                (int)OutputDeviceInfo.DefaultSampleRate,
                OnPortAudioReadSamples);
        }
        else
        {
            audioSource.Play();
        }
    }

    public void Stop()
    {
        if (UsePortAudio)
        {
            PortAudioUtils.StopPlayback(OutputDeviceInfo);
        }
        else
        {
            audioSource.Stop();
        }
    }

    /**
     * Use OnAudioFilterRead for low playback latency.
     * In contrast, an AudioClip in Unity always buffers first, which causes a delay before playback starts.
     */
    private void OnAudioFilterRead(float[] data, int channelCount)
    {
        if (SkipAudioOutput
            || UsePortAudio)
        {
            return;
        }

        sineToneGenerator.FillBuffer(data, channelCount);
    }

    /**
     * PortAudio host API has less latency than Unity's API.
     */
    private void OnPortAudioReadSamples(float[] data)
    {
        if (SkipAudioOutput
            || !UsePortAudio)
        {
            Array.Clear(data, 0, data.Length);
            return;
        }

        sineToneGenerator.FillBuffer(data, OutputDeviceInfo.MaxOutputChannels);
    }
}
