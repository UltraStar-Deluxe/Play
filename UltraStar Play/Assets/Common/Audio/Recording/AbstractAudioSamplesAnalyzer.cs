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

    protected static bool IsAboveNoiseSuppressionThreshold(float[] audioSamplesBuffer, MicProfile micProfile)
    {
        float minThreshold = micProfile.NoiseSuppression / 100f;
        for (int index = 0; index < audioSamplesBuffer.Length; index++)
        {
            if (Math.Abs(audioSamplesBuffer[index]) >= minThreshold)
            {
                return true;
            }
        }
        return false;
    }

    abstract public PitchEvent ProcessAudioSamples(float[] audioSamplesBuffer, int samplesSinceLastFrame, MicProfile mic);

}