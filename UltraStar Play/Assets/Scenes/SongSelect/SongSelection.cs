public struct SongSelection
{
    public SongMeta SongMeta { get; private set; }
    public int SongIndex { get; private set; }
    public int SongsCount { get; private set; }

    public SongSelection(SongMeta songMeta, int songIndex, int songCount)
    {
        SongMeta = songMeta;
        SongIndex = songIndex;
        SongsCount = songCount;
    }
}