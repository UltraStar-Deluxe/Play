﻿using System;
using System.Collections.Generic;
using UniInject;
using UniRx;

public class SongEditorIssueAnalyzerControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private NoteAreaControl noteAreaControl;

    private readonly Subject<IReadOnlyCollection<SongIssue>> issuesEventStream = new();
    public IObservable<IReadOnlyCollection<SongIssue>> IssuesEventStream => issuesEventStream;

    private int lastIssueCount;

    public void OnInjectionFinished()
    {
        UpdateIssues();
        songMetaChangeEventStream
            // When there is no new change to the song for some time, then update the issues.
            .Throttle(new TimeSpan(0, 0, 0, 0, 500))
            .Subscribe(_ => UpdateIssues());
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
