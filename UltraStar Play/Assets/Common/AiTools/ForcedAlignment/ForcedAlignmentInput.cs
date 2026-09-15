public class ForcedAlignmentInput
{
    public string Lyrics { get; set; }
    public float[] MonoSamples { get; set; }
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public int SampleRate { get; set; }

    public ForcedAlignmentInput(string lyrics, float[] monoSamples, int startIndex, int endIndex, int sampleRate)
    {
        Lyrics = lyrics;
        MonoSamples = monoSamples;
        StartIndex = startIndex;
        EndIndex = endIndex;
        SampleRate = sampleRate;
    }
}
