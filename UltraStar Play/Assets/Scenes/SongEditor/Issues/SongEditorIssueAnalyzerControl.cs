using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;

public class SongEditorIssueAnalyzerControl : INeedInjection, IInjectionFinishedListener
{
    public const int MaxSongIssueCountPerMessage = 10;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private NoteAreaControl noteAreaControl;

    private readonly Subject<IReadOnlyCollection<SongIssue>> issuesEventStream = new();
    public IObservable<IReadOnlyCollection<SongIssue>> IssuesEventStream => issuesEventStream;
    
    public IReadOnlyCollection<SongIssue> Issues { get; private set; } = new List<SongIssue>();

    public void OnInjectionFinished()
    {
        songMetaChangeEventStream
            // When there is no new change to the song for some time, then update the issues.
            .Throttle(new TimeSpan(0, 0, 0, 0, 500))
            .Subscribe(_ => UpdateIssues());
        MainThreadDispatcher.StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(1,
            () => UpdateIssues()));
    }

    private void UpdateIssues()
    {
        IReadOnlyCollection<SongIssue> issues = SongMetaAnalyzer.AnalyzeIssues(songMeta, MaxSongIssueCountPerMessage);
        List<Pair<SongIssue>> zipped = issues
            .Zip(Issues, (a, b) => new Pair<SongIssue>(a,b))
            .ToList();
        if (!issues.SequenceEqual(Issues))
        {
            Issues = issues;
            issuesEventStream.OnNext(issues);
        }
    }
}
