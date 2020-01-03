using UnityEngine;

public static class SongIssueUtils
{
    public static Color GetColorForIssue(SongIssue issue)
    {
        switch (issue.Severity)
        {
            case ESongIssueSeverity.Warning: return Colors.yellow;
            case ESongIssueSeverity.Error: return Colors.red;
        }
        return Color.black;
    }
}