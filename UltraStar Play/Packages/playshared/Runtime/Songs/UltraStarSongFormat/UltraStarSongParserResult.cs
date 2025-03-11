using System.Collections.Generic;

public class UltraStarSongParserResult
{
    public UltraStarSongMeta SongMeta { get; private set; }
    public List<SongIssue> SongIssues { get; private set; }

    public UltraStarSongParserResult(UltraStarSongMeta songMeta, List<SongIssue> songIssues)
    {
        SongMeta = songMeta;
        SongIssues = songIssues;
    }
}
