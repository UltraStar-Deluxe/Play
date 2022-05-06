public interface IAudioSamplesAnalyzer
{
    void Enable();

    void Disable();

    PitchEvent ProcessAudioSamples(float[] sampleBuffer, int startIndexInclusive, int endIndexExclusive, MicProfile mic);

    bool ModifySamplesInPlace { get; set; }
}
