using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx;
using UnityEngine;

public class SongIssueManager : AbstractSingletonBehaviour
{
    private ConcurrentBag<SongIssue> allSongIssues = new();
    public bool HasSongIssues => allSongIssues.Count > 0;

    public static SongIssueManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<SongIssueManager>();

    private CancellationTokenSource songIssueScanCancellationTokenSource;
    public bool IsSongIssueScanStarted => songIssueScanCancellationTokenSource != null;
    public bool IsSongIssueScanFinished { get; private set; }

    private readonly Subject<SongIssueScanFinishedEvent> songIssueScanFinishedEventStream = new();
    public IObservable<SongIssueScanFinishedEvent> SongIssueScanFinishedEventStream => songIssueScanFinishedEventStream
        .ObserveOnMainThread();

    [InjectedInAwake]
    private Settings settings;

    [InjectedInAwake]
    private SongMetaManager songMetaManager;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void AwakeSingleton()
    {
        settings = SettingsManager.Instance.Settings;
        songMetaManager = SongMetaManager.Instance;

        songMetaManager.AddedSongMetaEventStream
            .Subscribe(songMeta =>
            {
                if (songMeta is IHasSongIssues hasSongIssues
                    && !hasSongIssues.SongIssues.IsNullOrEmpty())
                {
                    AddSongIssues(hasSongIssues.SongIssues);
                }
            })
            .AddTo(gameObject);

        songIssueScanFinishedEventStream
            .Subscribe(evt =>
            {
                IsSongIssueScanFinished = true;
            })
            .AddTo(gameObject);
    }

    private void ResetSongIssues()
    {
        // Stop old song scan
        songIssueScanCancellationTokenSource?.Cancel();
        songIssueScanCancellationTokenSource = null;
        IsSongIssueScanFinished = false;

        allSongIssues = new ConcurrentBag<SongIssue>();
    }

    public void AddSongIssues(IEnumerable<SongIssue> songIssues)
    {
        songIssues.ForEach(songIssue => AddSongIssue(songIssue));
    }

    public void AddSongIssue(SongIssue songIssue)
    {
        if (songIssue == null)
        {
            return;
        }
        allSongIssues.Add(songIssue);
    }

    public void ReloadSongIssues()
    {
        ResetSongIssues();

        songMetaManager.ScanSongsIfNotDoneYet();
        if (songMetaManager.IsSongScanFinished)
        {
            ScanSongIssues();
        }
        else
        {
            songMetaManager.SongScanFinishedEventStream
                .SubscribeOneShot(evt => ScanSongIssues());
        }
    }

    public IReadOnlyList<SongIssue> GetSongIssues()
    {
        return allSongIssues.ToList();
    }

    public IReadOnlyList<SongIssue> GetSongErrors()
    {
        return allSongIssues
            .Where(it => it.Severity is ESongIssueSeverity.Error)
            .ToList();
    }

    public IReadOnlyList<SongIssue> GetSongWarnings()
    {
        return allSongIssues
            .Where(it => it.Severity is ESongIssueSeverity.Warning)
            .ToList();
    }

    private async void ScanSongIssues()
    {
        await ScanSongIssuesJob().GetResultAsync();
    }

    private Job<SongIssueScanResult> ScanSongIssuesJob()
    {
        if (IsSongIssueScanStarted)
        {
            throw new InvalidOperationException("Already started song issue scan");
        }

        songIssueScanCancellationTokenSource?.Cancel();
        songIssueScanCancellationTokenSource = new();

        JobProgress jobProgress = new(songIssueScanCancellationTokenSource);
        Job<SongIssueScanResult> job = new(Translation.Get(R.Messages.job_searchSongIssues), songIssueScanCancellationTokenSource);
        JobManager.Instance.AddJob(job);
        job.SetAwaitable(() => ScanSongIssuesAsync(songIssueScanCancellationTokenSource.Token, jobProgress));
        return job;
    }

    private async Awaitable<SongIssueScanResult> ScanSongIssuesAsync(
        CancellationToken cancellationToken,
        JobProgress jobProgress)
    {
        IReadOnlyCollection<SongMeta> songMetas = songMetaManager.GetSongMetas();

        await Awaitable.BackgroundThreadAsync();
        List<SongIssue> songIssues = await new SongIssueScanner().ScanSongIssuesAsync(settings, songMetas, cancellationToken, jobProgress);
        AddSongIssues(songIssues);

        await Awaitable.MainThreadAsync();
        songIssueScanFinishedEventStream.OnNext(new SongIssueScanFinishedEvent());

        return new SongIssueScanResult(songIssues);
    }
}
