public class SongMetaLoadedVoicesEvent
{
    public SongMeta SongMeta { get; private set; }

    public SongMetaLoadedVoicesEvent(SongMeta songMeta)
    {
        SongMeta = songMeta;
    }
}
