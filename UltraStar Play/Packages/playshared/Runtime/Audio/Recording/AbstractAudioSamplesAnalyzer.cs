using System;

public abstract class AbstractAudioSamplesAnalyzer : IAudioSamplesAnalyzer
{
    public bool ModifySamplesInPlace { get; set; } = true;

    public abstract PitchEvent ProcessAudioSamples(float[] sampleBuffer, int startIndexInclusive, int endIndexExclusive, MicProfile mic);

    public static void ApplyAmplification(float[] sampleBuffer, int fromIndexInclusive, int toIndexExclusive, float amplificationFactor)
    {
        if (amplificationFactor == 1)
        {
            return;
        }

        for (int i = fromIndexInclusive; i < sampleBuffer.Length && i < toIndexExclusive; i++)
        {
            sampleBuffer[i] *= amplificationFactor;
        }
    }

    public static bool IsAboveNoiseSuppressionThreshold(float[] sampleBuffer, int startIndexInclusive, int endIndexExclusive, float noiseSuppression)
    {
        if (noiseSuppression == 0)
        {
            return true;
        }

        float minThreshold = noiseSuppression / 100f;
        for (int index = startIndexInclusive; index < endIndexExclusive; index++)
        {
            if (Math.Abs(sampleBuffer[index]) >= minThreshold)
            {
                return true;
            }
        }
        return false;
    }

    protected static int PreviousPowerOfTwo(int x)
    {
        x |= (x >> 1);
        x |= (x >> 2);
        x |= (x >> 4);
        x |= (x >> 8);
        x |= (x >> 16);
        return x - (x >> 1);
    }
}
