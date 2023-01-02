public class AudioSeparationFinishedEvent
{
    public SongMeta SongMeta { get; private set;  }

    public AudioSeparationFinishedEvent(SongMeta songMeta)
    {
        SongMeta = songMeta;
    }
}
