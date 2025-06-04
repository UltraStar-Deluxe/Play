public static class PlaylistUtils
{
    public static bool IsFavorite(PlaylistManager playlistManager, SongMeta songMeta)
    {
        return songMeta != null
               && (playlistManager.FavoritesPlaylist?.HasSongEntry(songMeta) ?? false);
    }
}
