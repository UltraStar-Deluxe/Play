public class ForcedAlignmentFinishedEvent
{
    public SongMeta SongMeta { get; private set;  }
    public ForcedAlignmentResult ForcedAlignmentResult { get; private set; }

    public ForcedAlignmentFinishedEvent(SongMeta songMeta, ForcedAlignmentResult forcedAlignmentResult)
    {
        SongMeta = songMeta;
        ForcedAlignmentResult = forcedAlignmentResult;
    }
}
