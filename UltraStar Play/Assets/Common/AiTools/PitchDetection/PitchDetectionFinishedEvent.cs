public class PitchDetectionFinishedEvent
{
    public SongMeta SongMeta { get; private set;  }
    public PitchDetectionResult PitchDetectionResult { get; private set; }

    public PitchDetectionFinishedEvent(SongMeta songMeta, PitchDetectionResult pitchDetectionResult)
    {
        SongMeta = songMeta;
        PitchDetectionResult = pitchDetectionResult;
    }
}
