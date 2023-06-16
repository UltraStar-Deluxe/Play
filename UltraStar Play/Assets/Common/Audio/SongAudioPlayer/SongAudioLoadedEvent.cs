public class SongAudioLoadedEvent
{
    public SongMeta SongMeta { get; private set; }
    public string AudioUri { get; private set; }

    public SongAudioLoadedEvent(SongMeta songMeta, string audioUri)
    {
        SongMeta = songMeta;
        AudioUri = audioUri;
    }
}
