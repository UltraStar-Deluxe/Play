using System;
using UnityEngine;

[Serializable]
public class SongIssue
{
    public static readonly Comparison<SongIssue> compareBySongMetaPath =
        (a, b) => string.Compare(
            SongMetaUtils.GetAbsoluteSongMetaPath(a.SongMeta),
            SongMetaUtils.GetAbsoluteSongMetaPath(b.SongMeta), StringComparison.InvariantCulture);
    public static readonly Comparison<SongIssue> compareBySongMetaArtistAndTitle =
        (a, b) => string.Compare(
            SongMetaUtils.GetArtistDashTitle(a.SongMeta),
            SongMetaUtils.GetArtistDashTitle(b.SongMeta), StringComparison.InvariantCulture);

    public SongMeta SongMeta { get; set; }
    public ESongIssueSeverity Severity { get; private set; }
    public string Message { get; private set; }
    public int StartBeat { get; private set; }
    public int EndBeat { get; private set; }

    public SongIssue()
    {
    }

    public SongIssue(ESongIssueSeverity severity, SongMeta songMeta, string message, int startBeat, int endBeat)
    {
        Severity = severity;
        SongMeta = songMeta;
        Message = message;
        StartBeat = startBeat;
        EndBeat = endBeat;
    }

    public static SongIssue CreateWarning(SongMeta songMeta, string message, int startBeat=-1, int endBeat=-1)
    {
        SongIssue issue = new(ESongIssueSeverity.Warning, songMeta, message, startBeat, endBeat);
        return issue;
    }

    public static SongIssue CreateError(SongMeta songMeta, string message, int startBeat=-1, int endBeat=-1)
    {
        SongIssue issue = new(ESongIssueSeverity.Error, songMeta, message, startBeat, endBeat);
        return issue;
    }

    public void Log()
    {
        string beatRangeInfo = StartBeat >= 0 && EndBeat >= 0
            ? $" (from beat {StartBeat}, until beat {EndBeat})"
            : "";
        string logMessage = Message + beatRangeInfo;
        if (Severity == ESongIssueSeverity.Warning)
        {
            Debug.LogWarning(logMessage);
        }
        else
        {
            Debug.LogError(logMessage);
        }
    }
}
