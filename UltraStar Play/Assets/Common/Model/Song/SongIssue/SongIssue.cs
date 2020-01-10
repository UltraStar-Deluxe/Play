using System;

[Serializable]
public class SongIssue
{
    public ESongIssueSeverity Severity { get; private set; }
    public string Message { get; private set; }
    public int StartBeat { get; private set; }
    public int EndBeat { get; private set; }

    public SongIssue()
    {
    }

    public SongIssue(ESongIssueSeverity severity, string message, int startBeat, int endBeat)
    {
        Severity = severity;
        Message = message;
        StartBeat = startBeat;
        EndBeat = endBeat;
    }

    public static SongIssue CreateWarning(string message, int startBeat, int endBeat)
    {
        SongIssue issue = new SongIssue(ESongIssueSeverity.Warning, message, startBeat, endBeat);
        return issue;
    }

    public static SongIssue CreateError(string message, int startBeat, int endBeat)
    {
        SongIssue issue = new SongIssue(ESongIssueSeverity.Error, message, startBeat, endBeat);
        return issue;
    }
}