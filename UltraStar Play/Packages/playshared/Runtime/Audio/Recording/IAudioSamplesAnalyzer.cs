public interface IAudioSamplesAnalyzer
{
    bool ModifySamplesInPlace { get; set; }
    PitchEvent ProcessAudioSamples(float[] sampleBuffer, int startIndexInclusive, int endIndexExclusive, MicProfile mic);
}
