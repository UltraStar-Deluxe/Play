public class SongAudioLoadedEvent : AbstractSongMediaLoadedEvent
{
    public SongAudioLoadedEvent(SongMeta songMeta, string mediaUri)
        : base(songMeta, mediaUri)
    {
    }
}
