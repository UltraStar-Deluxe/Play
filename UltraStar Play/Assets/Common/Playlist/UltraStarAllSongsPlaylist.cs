// Special playlist that includes all songs
public class UltraStarAllSongsPlaylist : UltraStarPlaylist
{
    public UltraStarAllSongsPlaylist()
        : base("")
    {
    }

    public override bool HasSongEntry(string artist, string title)
    {
        return true;
    }
}
