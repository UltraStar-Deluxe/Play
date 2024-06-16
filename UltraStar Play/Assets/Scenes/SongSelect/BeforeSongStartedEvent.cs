public class BeforeSongStartedEvent
{
    public SongMeta SongMeta { get; private set; }
    public string CancelReason { get; set; }

    public BeforeSongStartedEvent(SongMeta songMeta)
    {
        SongMeta = songMeta;
    }
}
