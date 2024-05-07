using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Events;
using UniRx;
using UnityEngine;

public class SongIssueManager : AbstractSingletonBehaviour
{
    private ConcurrentBag<SongIssue> allSongIssues = new();
    public bool HasSongIssues => allSongIssues.Count > 0;

    public static SongIssueManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SongIssueManager>();

    private static string UnitySupportedVideoFileExtensionsCsv => ApplicationUtils.unitySupportedVideoFiles.JoinWith(", ");
    private static string UnitySupportedAudioFileExtensionsCsv => ApplicationUtils.unitySupportedAudioFiles.JoinWith(", ");

    private IDisposable songIssueScanDisposable;
    public bool IsSongIssueScanStarted => songIssueScanDisposable != null;
    public bool IsSongIssueScanFinished { get; private set; }

    private readonly Subject<SongIssueScanFinishedEvent> songIssueScanFinishedEventStream = new();
    public IObservable<SongIssueScanFinishedEvent> SongIssueScanFinishedEventStream => songIssueScanFinishedEventStream
        .ObserveOnMainThread();

    [InjectedInAwake]
    private Settings settings;

    [InjectedInAwake]
    private SongMetaManager songMetaManager;

    private static CancellationTokenSource songIssueScanCancellationTokenSource;

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
        songIssueScanDisposable?.Dispose();
        songIssueScanDisposable = null;
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

    private void ScanSongIssues()
    {
        if (IsSongIssueScanStarted)
        {
            throw new IllegalStateException("Already started song issue scan");
        }

        IReadOnlyCollection<SongMeta> songMetas = songMetaManager.GetSongMetas();
        CancellationDisposable cancellationDisposable = new();

        Job job = JobManager.CreateAndAddJob(Translation.Get(R.Messages.job_searchSongIssues));
        job.OnCancel = () => cancellationDisposable.Dispose();
        job.SetStatus(EJobStatus.Running);

        songIssueScanDisposable = ObservableUtils.RunOnNewTaskAsObservableElements(async () =>
                {
                    List<SongIssue> songIssues = await ScanSongIssuesAsync(songMetas, job, cancellationDisposable.Token);
                    AddSongIssues(songIssues);
                    return songIssues;
                },
                cancellationDisposable)
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                job.SetResult(EJobResult.Error);
            })
            .DoOnCompleted(() =>
            {
                songIssueScanFinishedEventStream.OnNext(new SongIssueScanFinishedEvent());
                if (job.Status.Value is EJobStatus.Running)
                {
                    job.SetResult(EJobResult.Ok);
                }
            })
            .Subscribe(_ =>
            {
                Debug.Log("Song issue scan finished.");
            });
    }

    private async Task<List<SongIssue>> ScanSongIssuesAsync(
        IReadOnlyCollection<SongMeta> songMetas,
        Job job,
        CancellationToken cancellationToken)
    {
        List<SongIssue> result = new();

        int doneSongMetas = 0;
        foreach (SongMeta songMeta in songMetas)
        {
            doneSongMetas++;
            job.EstimatedCurrentProgressInPercent = (double)doneSongMetas / songMetas.Count;

            if (songMeta == null)
            {
                // This should not happen.
                continue;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                Debug.Log($"Cancelled song issue scan. Found {result.Count} issues in {songMetas.Count} songs.");
                return result;
            }

            IDisposable d = new DisposableStopwatch($"Searching issues of song '{songMeta.GetArtistDashTitle()}' took <ms> ms");

            // Search issues in song txt file
            try
            {
                List<SongIssue> songIssues = GetSongIssuesInSongFile(songMeta);
                result.AddRange(songIssues);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to search issues in file '{songMeta.FileInfo}' of song '{songMeta.GetArtistDashTitle()}': {ex.Message}");
            }

            // Search issues in used audio, video, image files
            try
            {
                List<SongIssue> mediaFormatSongIssues = GetSupportedMediaFormatIssues(
                    songMeta,
                    settings.FfmpegToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Never,
                    settings.VlcToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Never,
                    settings.CheckCodecIsSupported);
                result.AddRange(mediaFormatSongIssues);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to search supported media format issues of song '{songMeta.GetArtistDashTitle()}': {ex.Message}");
            }

            // TODO: Search issues via SongMetaAnalyzer that is used to show additional errors/warnings in the song editor (e.g. overlapping notes).
        }

        Debug.Log($"Finished song issue scan. Found {result.Count} issues in {songMetas.Count} songs.");
        job.SetResult(EJobResult.Ok);
        return result;
    }

    private static List<SongIssue> GetSongIssuesInSongFile(SongMeta songMeta)
    {
        List<SongIssue> result = new();

        if (songMeta.FileInfo != null
            && songMeta.FileInfo.Exists)
        {
            SongMeta _ = UltraStarSongParser.ParseFile(songMeta.FileInfo.FullName, out List<SongIssue> songIssuesInFile, songMeta.FileEncoding, true, false);
            result.AddRange(songIssuesInFile);
        }

        return result;
    }

    /**
     * Checks whether the audio and video file formats of the song are supported.
     * Returns true iff the audio file of the SongMeta exists and is supported.
     */
    private static List<SongIssue> GetSupportedMediaFormatIssues(
        SongMeta songMeta,
        bool useFfmpegToPlayMediaFiles,
        bool useVlcToPlayMediaFiles,
        bool checkCodecIsSupported)
    {
        List<SongIssue> songIssues = new();

        // Check video exists and uses a supported format.
        if (SongMetaUtils.GetWebViewUrl(songMeta).IsNullOrEmpty())
        {
            string videoUri = SongMetaUtils.GetVideoUriPreferAudioUriIfWebView(songMeta, WebViewUtils.CanHandleWebViewUrl);
            CheckResourceExists(songIssues, songMeta, videoUri,
                () => Translation.Get(R.Messages.songIssue_media_notFound,
                    "value", ApplicationUtils.ReplacePathsWithDisplayString(videoUri)),
                ESongIssueSeverity.Warning);

            CheckVideoFormatIsSupported(songIssues, videoUri,
                () => Translation.Get(R.Messages.songIssue_media_unsupported,
                    "actual", GetUriOrExtensionWithoutDot(videoUri),
                    "expected", UnitySupportedVideoFileExtensionsCsv),
                () => new FormatNotSupportedSongIssueData(songMeta, FormatNotSupportedSongIssueData.EMediaType.Video),
                ESongIssueSeverity.Warning);
        }

        if (useFfmpegToPlayMediaFiles
            && !useVlcToPlayMediaFiles)
        {
            // The ffmpeg integration in Unity can at the moment only play one file.
            // Thus, check video file is either same as audio file or ffmpeg is not used to play it.
            bool isVideoEmptyOrSameAsAudio = songMeta.Video.IsNullOrEmpty()
                                             || string.Equals(songMeta.Video, songMeta.Audio, StringComparison.InvariantCultureIgnoreCase);
            if (!isVideoEmptyOrSameAsAudio
                && !ApplicationUtils.IsUnitySupportedVideoFormat(Path.GetExtension(songMeta.Video))
                && !WebViewUtils.CanHandleWebViewUrl(songMeta.Video))
            {
                songIssues.Add(SongIssue.CreateWarning(songMeta, Translation.Get(R.Messages.songIssue_media_videoDiffersFromAudio,
                    "supportedFormats", UnitySupportedVideoFileExtensionsCsv)));

                // Do not attempt to load this video file, it will not work.
                SongVideoPlayer.AddIgnoredVideoFile(songMeta.Video);
            }
        }

        if (checkCodecIsSupported
            && !useFfmpegToPlayMediaFiles
            && !useVlcToPlayMediaFiles)
        {
            CheckVideoCodecsAreSupportedByUnity(songIssues, songMeta);
        }

        // Check audio format.
        // Audio is mandatory. Without working audio file, the song cannot be played.
        string audioUri = SongMetaUtils.GetAudioUri(songMeta);
        if (SongMetaUtils.GetWebViewUrl(songMeta).IsNullOrEmpty())
        {
            // Must have local audio file in supported format because no website is specified.
            CheckResourceExists(songIssues, songMeta, audioUri,
                () => Translation.Get(R.Messages.songIssue_media_notFound,
                    "value", ApplicationUtils.ReplacePathsWithDisplayString(audioUri)),
                    ESongIssueSeverity.Error);
            CheckAudioOrVideoFormatIsSupported(songIssues, audioUri,
                () => Translation.Get(R.Messages.songIssue_media_unsupported,
                    "actual", GetUriOrExtensionWithoutDot(audioUri),
                    "expected", UnitySupportedAudioFileExtensionsCsv),
                () => new FormatNotSupportedSongIssueData(songMeta, FormatNotSupportedSongIssueData.EMediaType.Audio),
                ESongIssueSeverity.Error);
        }
        // Check WebView URI is supported when needed
        else if (!SongMetaUtils.ResourceExists(songMeta, songMeta.Audio))
        {
            string webViewUrl = SongMetaUtils.GetWebViewUrl(songMeta);
            if (!WebViewUtils.CanHandleWebViewUrl(webViewUrl))
            {
                // Cannot use the local audio file and not the website. This song cannot be played.
                songIssues.Add(SongIssue.CreateError(songMeta, Translation.Get(R.Messages.songIssue_media_missingAudioAndUnsupportedWebsite,
                    "audio", ApplicationUtils.ReplacePathsWithDisplayString(songMeta.Audio),
                    "website", webViewUrl)));
            }
        }

        // Vocals audio and instrumental audio must use formats that are supported by Unity. Ffmpeg can only be used for the main audio.
        string vocalsAudioUri = SongMetaUtils.GetVocalsAudioUri(songMeta);
        CheckResourceExists(songIssues, songMeta, vocalsAudioUri,
            () => Translation.Get(R.Messages.songIssue_media_notFound,
                "value", ApplicationUtils.ReplacePathsWithDisplayString(vocalsAudioUri)),
            ESongIssueSeverity.Warning);
        CheckIsUnitySupportedAudioFormat(songIssues, songMeta.VocalsAudio,
            () => Translation.Get(R.Messages.songIssue_media_unsupported,
                "actual", GetUriOrExtensionWithoutDot(vocalsAudioUri),
                "expected", UnitySupportedAudioFileExtensionsCsv),
            () => new FormatNotSupportedSongIssueData(songMeta, FormatNotSupportedSongIssueData.EMediaType.VocalsAudio),
            ESongIssueSeverity.Warning);

        string instrumentalAudioUri = SongMetaUtils.GetInstrumentalAudioUri(songMeta);
        CheckResourceExists(songIssues, songMeta, instrumentalAudioUri,
            () => Translation.Get(R.Messages.songIssue_media_notFound,
                "value", ApplicationUtils.ReplacePathsWithDisplayString(instrumentalAudioUri)),
            ESongIssueSeverity.Warning);
        CheckIsUnitySupportedAudioFormat(songIssues, songMeta.InstrumentalAudio,
            () => Translation.Get(R.Messages.songIssue_media_unsupported,
                "actual", GetUriOrExtensionWithoutDot(instrumentalAudioUri),
                "expected", UnitySupportedAudioFileExtensionsCsv),
            () => new FormatNotSupportedSongIssueData(songMeta, FormatNotSupportedSongIssueData.EMediaType.InstrumentalAudio),
            ESongIssueSeverity.Warning);

        // Log found issues
        songIssues.ForEach(songIssue => songIssue.Log());

        return songIssues;
    }


    private static void CheckVideoCodecsAreSupportedByUnity(List<SongIssue> songIssues, SongMeta songMeta)
    {
        if (!songMeta.Audio.IsNullOrEmpty())
        {
            CheckVideoCodecIsSupported(songIssues, songMeta, songMeta.Audio,
                codec => Translation.Get(R.Messages.songIssue_media_unsupported,
                    "actual", codec,
                    "expected", UnitySupportedVideoFileExtensionsCsv),
                () => new FormatNotSupportedSongIssueData(songMeta, FormatNotSupportedSongIssueData.EMediaType.Video),
                ESongIssueSeverity.Error);
        }

        if (!songMeta.Video.IsNullOrEmpty())
        {
            CheckVideoCodecIsSupported(songIssues, songMeta, songMeta.Video,
                codec => Translation.Get(R.Messages.songIssue_media_unsupported,
                    "actual", codec,
                    "expected", UnitySupportedVideoFileExtensionsCsv),
                () => new FormatNotSupportedSongIssueData(songMeta, FormatNotSupportedSongIssueData.EMediaType.Video),
                ESongIssueSeverity.Warning);
        }
    }

    private static void CheckVideoCodecIsSupported(
        List<SongIssue> songIssues,
        SongMeta songMeta,
        string pathOrUri,
        Func<string, Translation> errorMessageGetter,
        Func<SongIssueData> songIssueDataGetter,
        ESongIssueSeverity severity)
    {
        string videoFilePath = SongMetaUtils.GetAbsoluteFilePath(songMeta, pathOrUri);
        if (!FileUtils.Exists(videoFilePath))
        {
            return;
        }

        string videoFileExtension = PathUtils.GetExtensionWithoutDot(videoFilePath)
            .ToLowerInvariant();
        if (!ApplicationUtils.IsSupportedVideoFormat(videoFileExtension))
        {
            return;
        }

        if (videoFileExtension == "webm"
            || videoFileExtension == "mp4")
        {
            string ffprobeArguments = "-v error -select_streams v:0 -show_entries stream=codec_name -of default=noprint_wrappers=1:nokey=1 \"INPUT_FILE\"";
            ProcessUtils.RunProcess(
                ApplicationUtils.GetStreamingAssetsPath("ffmpeg/ffprobe.exe"),
                ffprobeArguments.Replace("INPUT_FILE", videoFilePath),
                out string ffprobeOutput,
                out string ffprobeErrorOutput,
                LogEventLevel.Verbose,
                LogEventLevel.Verbose);

            string codec = ffprobeOutput.Trim().ToLowerInvariant();
            if (codec == "vp9"
                || codec == "av1")
            {
                songIssues.Add(new SongIssue(severity, songIssueDataGetter(), errorMessageGetter(codec), -1, -1));
            }
        }
    }

    private static string GetUriOrExtensionWithoutDot(string pathOrUri)
    {
        if (WebRequestUtils.IsHttpOrHttpsUri(pathOrUri))
        {
            return pathOrUri;
        }
        return PathUtils.GetExtensionWithoutDot(pathOrUri);
    }

    private static void CheckResourceExists(
        List<SongIssue> songIssues,
        SongMeta songMeta,
        string pathOrUri,
        Func<Translation> errorMessageGetter,
        ESongIssueSeverity severity)
    {
        if (pathOrUri.IsNullOrEmpty())
        {
            return;
        }

        if (!SongMetaUtils.ResourceExists(songMeta, pathOrUri))
        {
            songIssues.Add(new SongIssue(severity, new SongIssueData(songMeta), errorMessageGetter(), -1, -1));
        }
    }

    private static void CheckIsUnitySupportedAudioFormat(
        List<SongIssue> songIssues,
        string pathOrUri,
        Func<Translation> errorMessageGetter,
        Func<SongIssueData> songIssueDataGetter,
        ESongIssueSeverity severity)
    {
        if (pathOrUri.IsNullOrEmpty())
        {
            return;
        }

        if (!ApplicationUtils.IsUnitySupportedAudioFormat(Path.GetExtension(pathOrUri)))
        {
            songIssues.Add(new SongIssue(severity, songIssueDataGetter(), errorMessageGetter(), -1, -1));
        }
    }

    private static void CheckVideoFormatIsSupported(
        List<SongIssue> songIssues,
        string pathOrUri,
        Func<Translation> errorMessageGetter,
        Func<SongIssueData> songIssueDataGetter,
        ESongIssueSeverity severity)
    {
        if (pathOrUri.IsNullOrEmpty())
        {
            return;
        }

        if (!ApplicationUtils.IsSupportedVideoFormat(Path.GetExtension(pathOrUri))
            && !WebViewUtils.CanHandleWebViewUrl(pathOrUri))
        {
            songIssues.Add(new SongIssue(severity, songIssueDataGetter(), errorMessageGetter(), -1, -1));
            // Do not attempt to load this file
            SongVideoPlayer.AddIgnoredVideoFile(pathOrUri);
        }
    }

    private static void CheckAudioOrVideoFormatIsSupported(
        List<SongIssue> songIssues,
        string pathOrUri,
        Func<Translation> errorMessageGetter,
        Func<SongIssueData> songIssueDataGetter,
        ESongIssueSeverity severity)
    {
        if (pathOrUri.IsNullOrEmpty())
        {
            return;
        }

        string fileExtension = Path.GetExtension(pathOrUri);
        if (!ApplicationUtils.IsSupportedAudioFormat(fileExtension)
            && !ApplicationUtils.IsSupportedVideoFormat(fileExtension)
            && !WebViewUtils.CanHandleWebViewUrl(pathOrUri))
        {
            songIssues.Add(new SongIssue(severity, songIssueDataGetter(), errorMessageGetter(), -1, -1));
        }
    }
}
