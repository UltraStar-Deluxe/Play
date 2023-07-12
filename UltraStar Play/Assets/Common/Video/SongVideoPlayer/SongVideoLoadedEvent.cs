public class SongVideoLoadedEvent
{
    public SongMeta SongMeta { get; private set; }
    public string VideoUri { get; private set; }

    public SongVideoLoadedEvent(SongMeta songMeta, string videoUri)
    {
        SongMeta = songMeta;
        VideoUri = videoUri;
    }    
}
