using System;
using System.Collections.Generic;

public class MicRecordingData
{
    // Static instance to be persisted across scenes
    public static Dictionary<PlayerProfile, MicRecordingData> PlayerProfileToMicRecording { get; private set; } = new Dictionary<PlayerProfile, MicRecordingData>();

    public PlayerProfile PlayerProfile { get; private set; }
    public float[] MicSamples { get; private set; }
    public int MicSampleRate { get; private set; }
    public int OverallDelayInMillis { get; private set; }
    public int WrittenMicSampleCount { get; private set; }

    public MicRecordingData(PlayerProfile playerProfile, float[] micSamples, int micSampleRate, int overallMicDelayInMillis)
    {
        PlayerProfile = playerProfile;
        MicSamples = micSamples;
        MicSampleRate = micSampleRate;
        OverallDelayInMillis = overallMicDelayInMillis;
    }

    public void AddSamples(RecordingEvent evt)
    {
        Array.Copy(evt.MicSamples, evt.NewSamplesStartIndex, MicSamples, WrittenMicSampleCount, evt.NewSampleCount);
        WrittenMicSampleCount += evt.NewSampleCount;
    }
}