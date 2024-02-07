public static class ESongVideoPlaybackExtensions
{
    public static string ToDisplayString(this ESongVideoPlayback item)
    {
        switch (item)
        {
            case ESongVideoPlayback.DisabledInSongSelectAndSing:
                return "Disabled In Song Select And When Singing";
            default:
                return StringUtils.ToTitleCase(item.ToString());
        }
    }
}
