public class SpeechRecognitionInputSamples
{
    public float[] MonoSamples { get; set; }
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public int SampleRate { get; set; }

    public SpeechRecognitionInputSamples(
        float[] monoSamples,
        int startIndex,
        int endIndex,
        int sampleRate)
    {
        MonoSamples = monoSamples;
        StartIndex = startIndex;
        EndIndex = endIndex;
        SampleRate = sampleRate;
    }
}
