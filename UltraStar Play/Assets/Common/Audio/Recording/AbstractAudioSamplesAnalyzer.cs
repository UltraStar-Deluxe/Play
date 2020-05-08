using System;

abstract public class AbstractAudioSamplesAnalyzer : IAudioSamplesAnalyzer
{
    protected bool isEnabled;

    public void Enable()
    {
        isEnabled = true;
    }

    public void Disable()
    {
        isEnabled = false;
    }

    protected static bool IsAboveNoiseSuppressionThreshold(float[] samplesBuffer, int sampleStartIndex, int sampleEndIndex, MicProfile micProfile)
    {
        float minThreshold = micProfile.NoiseSuppression / 100f;
        for (int index = sampleStartIndex; index < sampleEndIndex; index++)
        {
            if (Math.Abs(samplesBuffer[index]) >= minThreshold)
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

    abstract public PitchEvent ProcessAudioSamples(float[] sampleBuffer, int sampleStartIndex, int sampleEndIndex, MicProfile mic);

}
