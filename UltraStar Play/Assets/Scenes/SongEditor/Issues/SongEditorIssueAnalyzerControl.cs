using System;
using System.Collections.Generic;
using UniRx;
using UniInject;
using UnityEngine;

public class SongEditorIssueAnalyzerControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private NoteAreaControl noteAreaControl;

    private readonly Subject<IReadOnlyCollection<SongIssue>> issuesEventStream = new Subject<IReadOnlyCollection<SongIssue>>();
    public IObservable<IReadOnlyCollection<SongIssue>> IssuesEventStream => issuesEventStream;

    private int lastIssueCount;

    public void OnInjectionFinished()
    {
        UpdateIssues();
        songMetaChangeEventStream.Subscribe(_ => UpdateIssues());
    }

    private void UpdateIssues()
    {
        IReadOnlyCollection<SongIssue> issues = SongMetaAnalyzer.AnalyzeIssues(songMeta);
        if (issues.Count != lastIssueCount)
        {
            issuesEventStream.OnNext(issues);
        }
        lastIssueCount = issues.Count;
    }
}
