public static class PlaylistUtils
{
    public static bool IsFavorite(PlaylistManager playlistManager, SongMeta songMeta)
    {
        return songMeta != null
               && IsInPlaylist(playlistManager.FavoritesPlaylist, songMeta);
    }
    
    public static bool IsInPlaylist(IPlaylist playlist, SongMeta songMeta)
    {
        return songMeta != null
               && (playlist?.HasSongEntry(songMeta) ?? false);
    }
}
