using System.Collections.Generic;

public class FoundSongIssuesEvent : ICommonEvent
{
    public List<SongIssue> SongIssues { get; private set; }

    public FoundSongIssuesEvent(List<SongIssue> songIssues)
    {
        SongIssues = songIssues;
    }

    public FoundSongIssuesEvent(SongIssue songIssue)
    {
        SongIssues = new List<SongIssue>() { songIssue };
    }
}
