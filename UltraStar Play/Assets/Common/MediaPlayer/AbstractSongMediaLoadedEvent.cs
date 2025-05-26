public abstract class AbstractSongMediaLoadedEvent : ISongMediaLoadedEvent
{
    public SongMeta SongMeta { get; private set; }
    public string MediaUri { get; }

    public AbstractSongMediaLoadedEvent(SongMeta songMeta, string mediaUri)
    {
        SongMeta = songMeta;
        MediaUri = mediaUri;
    }
}
