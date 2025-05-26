using System;
using UnityEngine;

[Serializable]
public class SongIssue
{
    public static readonly Comparison<SongIssue> compareBySongMetaArtistAndTitle =
        (a, b) => string.Compare(
            a.SongMeta.GetArtistDashTitle(),
            b.SongMeta.GetArtistDashTitle(), StringComparison.InvariantCulture);

    public SongIssueData SongIssueData { get; private set; }
    public SongMeta SongMeta => SongIssueData?.SongMeta;
    public ESongIssueSeverity Severity { get; private set; }
    public Translation Message { get; private set; }
    public int StartBeat { get; private set; }
    public int EndBeat { get; private set; }

    public SongIssue()
    {
    }

    public SongIssue(
        ESongIssueSeverity severity,
        SongIssueData songIssueData,
        Translation message,
        int startBeat,
        int endBeat)
    {
        Severity = severity;
        SongIssueData = songIssueData;
        Message = message;
        StartBeat = startBeat;
        EndBeat = endBeat;
        SongIssueData = songIssueData;
    }

    public static SongIssue CreateWarning(
        SongMeta songMeta,
        Translation message,
        int startBeat = -1,
        int endBeat = -1)
    {
        SongIssue issue = new(ESongIssueSeverity.Warning, new SongIssueData(songMeta), message, startBeat, endBeat);
        return issue;
    }

    public static SongIssue CreateError(
        SongMeta songMeta,
        Translation message,
        int startBeat = -1,
        int endBeat = -1)
    {
        SongIssue issue = new(ESongIssueSeverity.Error, new SongIssueData(songMeta), message, startBeat, endBeat);
        return issue;
    }

    public void Log()
    {
        string beatRangeInfo = StartBeat >= 0 && EndBeat >= 0
            ? $" (from beat {StartBeat}, until beat {EndBeat})"
            : "";
        string songMetaInfo = $" (in song '{SongMeta.GetArtistDashTitle()}')";
        string logMessage = $"{Message}{beatRangeInfo}{songMetaInfo}";
        if (Severity == ESongIssueSeverity.Warning)
        {
            Debug.LogWarning(logMessage);
        }
        else
        {
            Debug.LogError(logMessage);
        }
    }

    public override bool Equals(object obj)
    {
        if (obj is not SongIssue y)
        {
            return false;
        }

        SongIssue x = this;
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        return Equals(x.SongMeta, y.SongMeta)
               && x.Severity == y.Severity
               && x.Message == y.Message
               && x.StartBeat == y.StartBeat
               && x.EndBeat == y.EndBeat;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SongMeta, (int)Severity, Message, StartBeat, EndBeat);
    }
}
