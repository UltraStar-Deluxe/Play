public class RecordingEvent
{
    public float[] MicSamples { get; private set; }
    public int NewSamplesStartIndex { get; private set; }
    public int NewSamplesEndIndex { get; private set; }
    public int NewSampleCount => NewSamplesEndIndex - NewSamplesStartIndex;
    
    public RecordingEvent(float[] micBuffer, int newSamplesStartIndex, int newSamplesEndIndex)
    {
        MicSamples = micBuffer;
        NewSamplesStartIndex = newSamplesStartIndex;
        NewSamplesEndIndex = newSamplesEndIndex;
    }
}
