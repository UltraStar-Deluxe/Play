public class SongVideoLoadedEvent : AbstractSongMediaLoadedEvent
{
    public SongVideoLoadedEvent(SongMeta songMeta, string mediaUri)
        : base(songMeta, mediaUri)
    {
    }
}
