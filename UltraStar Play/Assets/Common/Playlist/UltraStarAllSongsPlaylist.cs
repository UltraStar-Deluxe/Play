// Special playlist that includes all songs
public class UltraStarAllSongsPlaylist : UltraStarPlaylist
{
    public static UltraStarAllSongsPlaylist Instance { get; private set; } = new();
    public override bool IsEmpty => SongMetaManager.Instance.GetSongMetas().IsNullOrEmpty();

    public override bool HasSongEntry(string artist, string title)
    {
        return true;
    }

    private UltraStarAllSongsPlaylist()
    {
    }
}
