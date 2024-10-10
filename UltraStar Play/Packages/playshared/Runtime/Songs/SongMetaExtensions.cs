public static class SongMetaExtensions
{
    public static string GetArtistDashTitle(this SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return "";
        }
        return SongMetaUtils.GetArtistDashTitle(songMeta);
    }
}
