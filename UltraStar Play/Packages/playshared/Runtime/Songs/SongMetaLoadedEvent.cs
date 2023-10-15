public class SongMetaLoadedEvent : ICommonEvent
{
    public SongMeta SongMeta { get; private set; }

    public SongMetaLoadedEvent(SongMeta songMeta)
    {
        SongMeta = songMeta;
    }
}
