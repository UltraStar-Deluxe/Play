using System.Collections.Generic;

public class FoundSongMetaIssuesEvent
{
    public List<SongIssue> SongIssues { get; private set; }

    public FoundSongMetaIssuesEvent(List<SongIssue> songIssues)
    {
        SongIssues = songIssues;
    }

    public FoundSongMetaIssuesEvent(SongIssue songIssue)
        : this(new List<SongIssue>() { songIssue })
    {
    }
}
