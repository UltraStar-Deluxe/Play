using PortAudioForUnity;
using UnityEngine;

public class PortAudioForUnityMicrophoneAdapter : IMicrophoneAdapter
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        IMicrophoneAdapter.Instance = new PortAudioForUnityMicrophoneAdapter();
    }

    public bool UsePortAudio
    {
        get => MicrophoneAdapter.UsePortAudio;
        set => MicrophoneAdapter.UsePortAudio = value;
    }

    public DeviceInfo DefaultInputDeviceInfo => MicrophoneAdapter.DefaultInputDeviceInfo;
    public DeviceInfo DefaultOutputDeviceInfo => MicrophoneAdapter.DefaultOutputDeviceInfo;
    public string[] Devices => MicrophoneAdapter.Devices;
    public HostApi GetHostApi() => MicrophoneAdapter.GetHostApi();
    public HostApiInfo GetHostApiInfo() => MicrophoneAdapter.GetHostApiInfo();
    public void SetHostApi(HostApi hostApi) => MicrophoneAdapter.SetHostApi(hostApi);

    public bool IsRecording(string deviceName) => MicrophoneAdapter.IsRecording(deviceName);

    public AudioClip Start(
        string inputDeviceName,
        bool loop,
        int bufferLengthSec,
        int sampleRate,
        string outputDeviceName = "",
        float directOutputAmplificationFactor = 1)
        => MicrophoneAdapter.Start(inputDeviceName,
            loop,
            bufferLengthSec,
            sampleRate,
            outputDeviceName,
            directOutputAmplificationFactor);

    public void End(string deviceName) => MicrophoneAdapter.End(deviceName);

    public int GetPosition(string deviceName) => MicrophoneAdapter.GetPosition(deviceName);

    public void GetRecordedSamples(
        string deviceName,
        int channelIndex,
        AudioClip microphoneAudioClip,
        int recordingPosition,
        float[] bufferToBeFilled)
        => MicrophoneAdapter.GetRecordedSamples(
            deviceName,
            channelIndex,
            microphoneAudioClip,
            recordingPosition,
            bufferToBeFilled);

    public void GetDeviceCaps(
        string deviceName,
        out int minFreq,
        out int maxFreq,
        out int channelCount)
        => MicrophoneAdapter.GetDeviceCaps(
            deviceName,
            out minFreq,
            out maxFreq,
            out channelCount);
}
