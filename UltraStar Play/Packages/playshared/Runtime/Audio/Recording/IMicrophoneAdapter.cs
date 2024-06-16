using UnityEngine;

public interface IMicrophoneAdapter
{
    public static IMicrophoneAdapter Instance { get; set; }

    public bool UsePortAudio { get; set; }

    public string[] Devices { get; }

    public void GetDeviceCaps(
        string deviceName,
        out int minFreq,
        out int maxFreq,
        out int channelCount);

    public bool IsRecording(string deviceName);

    public AudioClip Start(
        string inputDeviceName,
        bool loop,
        int bufferLengthSec,
        int sampleRate,
        string outputDeviceName = "",
        float directOutputAmplificationFactor = 1);
    public void End(string deviceName);

    public int GetPosition(string deviceName);

    public void GetRecordedSamples(
        string deviceName,
        int channelIndex,
        AudioClip microphoneAudioClip,
        int recordingPosition,
        float[] bufferToBeFilled);

}
