public interface IPlaylist
{
    string Name { get; }
    string FileName { get; }
    string FilePath { get; }
    bool IsEmpty { get; }
    int Count { get; }
    bool HasSongEntry(SongMeta songMeta);
}
