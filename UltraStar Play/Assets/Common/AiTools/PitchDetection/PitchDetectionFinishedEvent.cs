public class PitchDetectionFinishedEvent
{
    public SongMeta SongMeta { get; private set;  }

    public PitchDetectionFinishedEvent(SongMeta songMeta)
    {
        SongMeta = songMeta;
    }
}
