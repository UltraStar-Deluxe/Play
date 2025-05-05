using System.Collections.Generic;

public class SongRepositorySearchResultEntry
{
    public SongMeta SongMeta { get; set; }
    public List<SongIssue> SongIssues { get; set; }

    public SongRepositorySearchResultEntry(
        SongMeta songMeta,
        List<SongIssue> songIssues)
    {
        SongMeta = songMeta;
        SongIssues = songIssues;
    }
}
