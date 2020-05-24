// Special playlist that includes all songs
public class UltraStarAllSongsPlaylist : UltraStarPlaylist
{
    public override bool HasSongEntry(string artist, string title)
    {
        return true;
    }
}
