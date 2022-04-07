public interface IAudioSamplesAnalyzer
{
    void Enable();

    void Disable();

    PitchEvent ProcessAudioSamples(float[] sampleBuffer, int sampleStartIndex, int sampleEndIndex, MicProfile mic);
}
