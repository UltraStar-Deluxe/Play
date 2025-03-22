public interface ISongMediaLoadedEvent
{
    public SongMeta SongMeta { get; }
    public string MediaUri { get; }
}
