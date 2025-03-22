using System.Collections.Generic;

public class SongIssueScanResult
{
    public List<SongIssue> Issues { get; private set; }

    public SongIssueScanResult(List<SongIssue> issues)
    {
        Issues = issues;
    }
}
