public class AudioSamplesAnalyzerFactory
{
    public static IAudioSamplesAnalyzer Create(EPitchDetectionAlgorithm pitchDetectionAlgorithm, int sampleRateHz)
    {
        switch (pitchDetectionAlgorithm)
        {
            case EPitchDetectionAlgorithm.Camd:
                CamdAudioSamplesAnalyzer camdAudioSamplesAnalyzer = new(sampleRateHz, PitchDetectionConstants.LongestSingableNoteSampleCount);
                return camdAudioSamplesAnalyzer;
            case EPitchDetectionAlgorithm.Dywa:
                DywaAudioSamplesAnalyzer dywaAudioSamplesAnalyzer = new(sampleRateHz, PitchDetectionConstants.LongestSingableNoteSampleCount);
                return dywaAudioSamplesAnalyzer;
            default:
                throw new IllegalArgumentException($"Unknown pitch detection algorithm: {pitchDetectionAlgorithm}");
        }
    }
}
