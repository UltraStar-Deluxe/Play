public class SongScanFinishedEvent
{
    public int LoadedSongCount { get; private set; }

    public SongScanFinishedEvent(int loadedSongCount)
    {
        LoadedSongCount = loadedSongCount;
    }
}
