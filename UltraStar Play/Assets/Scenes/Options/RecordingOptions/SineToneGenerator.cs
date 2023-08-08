using UnityEngine;

public class SineToneGenerator
{
    public int Frequency { get; set; }
    public int SampleRate { get; set; }
    private int totalMonoSampleIndex;

    public SineToneGenerator(int frequency, int sampleRate)
    {
        Frequency = frequency;
        SampleRate = sampleRate;
    }

    public void FillBuffer(float[] data, int channelCount)
    {
        for (int sampleIndex = 0; sampleIndex < data.Length; sampleIndex += channelCount)
        {
            for (int channelIndex = 0; channelIndex < channelCount; channelIndex++)
            {
                data[sampleIndex + channelIndex] = Mathf.Sin(2 * Mathf.PI * Frequency * totalMonoSampleIndex / SampleRate);
            }
            totalMonoSampleIndex++;
        }
    }
}
