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
    private static ConcurrentBag<SongIssue> allSongIssues = new();
    public static bool HasSongIssues => allSongIssues.Count > 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        ResetSongIssues();
    }

    public static SongIssueManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SongIssueManager>();

    private static readonly string unitySupportedVideoFileExtensionsAsCsv = ApplicationUtils.unitySupportedVideoFiles.ToCsv(",", "", "");
    private static readonly string unitySupportedAudioFileExtensionsAsCsv = ApplicationUtils.unitySupportedAudioFiles.ToCsv(",", "", "");

    [InjectedInAwake]
    private Settings settings;

    [InjectedInAwake]
    private SongMetaManager songMetaManager;

    private readonly Subject<SongIssueScanFinishedEvent> songIssueScanFinishedEventStream = new();
    public IObservable<SongIssueScanFinishedEvent> SongIssueScanFinishedEventStream => songIssueScanFinishedEventStream;

    private static CancellationTokenSource songIssueScanCancellationTokenSource;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void AwakeSingleton()
    {
        settings = SettingsManager.Instance.Settings;
        songMetaManager = SongMetaManager.Instance;
    }

    private static void ResetSongIssues()
    {
        allSongIssues = new ConcurrentBag<SongIssue>();
    }

    public void ReloadSongIssues()
    {
        ResetSongIssues();
        if (SongMetaManager.IsSongScanFinished)
        {
            ScanSongIssues();
        }
        else
        {
            IDisposable songScanFinishedEventStreamDisposable = null;
            songScanFinishedEventStreamDisposable = songMetaManager.SongScanFinishedEventStream
                .Subscribe(evt =>
                {
                    ScanSongIssues();
                    songScanFinishedEventStreamDisposable?.Dispose();
                });
        }
    }

    public static void AddSongIssue(SongIssue songIssue)
    {
        if (songIssue == null)
        {
            return;
        }
        allSongIssues.Add(songIssue);
    }

    public static void AddSongIssues(IReadOnlyCollection<SongIssue> songIssues)
    {
        songIssues.ForEach(AddSongIssue);
    }

    public static IReadOnlyList<SongIssue> GetSongIssues()
    {
        return allSongIssues.ToList();
    }

    public static IReadOnlyList<SongIssue> GetSongErrors()
    {
        return allSongIssues
            .Where(it => it.Severity is ESongIssueSeverity.Error)
            .ToList();
    }

    public static IReadOnlyList<SongIssue> GetSongWarnings()
    {
        return allSongIssues
            .Where(it => it.Severity is ESongIssueSeverity.Warning)
            .ToList();
    }

    private void ScanSongIssues()
    {
        IReadOnlyCollection<SongMeta> songMetas = songMetaManager.GetSongMetas();
        CancellationDisposable cancellationDisposable = new();

        Job job = JobManager.CreateAndAddJob("Search issues in songs");
        job.OnCancel = () => cancellationDisposable.Dispose();
        job.SetStatus(EJobStatus.Running);

        ObservableUtils.RunOnNewTaskAsObservableElements(
            async () => await ScanSongIssuesAsync(songMetas, job, cancellationDisposable.Token),
            cancellationDisposable)
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                job.SetResult(EJobResult.Error);
            })
            .DoOnCompleted(() =>
            {
                if (job.Status.Value is EJobStatus.Running)
                {
                    job.SetResult(EJobResult.Ok);
                }
            })
            .Subscribe(songIssue =>
            {
                AddSongIssue(songIssue);
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

            IDisposable d = new DisposableStopwatch($"Searching issues of song '{SongMetaUtils.GetArtistDashTitle(songMeta)}' took <ms> ms");

            // Search issues in song txt file
            try
            {
                List<SongIssue> songIssues = GetSongIssuesInSongFile(songMeta);
                result.AddRange(songIssues);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to search issues from file '{songMeta.FileInfo}' of song '{SongMetaUtils.GetArtistDashTitle(songMeta)}': {ex.Message}");
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
                Debug.LogError($"Failed to search supported media format issues of song '{SongMetaUtils.GetArtistDashTitle(songMeta)}': {ex.Message}");
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
        if (SongMetaUtils.GetWebsiteUri(songMeta).IsNullOrEmpty())
        {
            string videoUri = SongMetaUtils.GetVideoUriPreferAudioUriIfWebView(songMeta, WebViewUtils.CanHandleWebViewUrl);
            CheckResourceExists(songIssues, songMeta, videoUri,
                () => $"Video resource does not exist '{ApplicationUtils.ReplacePathsWithDisplayString(videoUri)}'",
                ESongIssueSeverity.Warning);

            CheckVideoFormatIsSupported(songIssues, videoUri,
                () => $"Unsupported video format '{GetUriOrExtensionWithoutDot(videoUri)}'. Convert to one of {unitySupportedVideoFileExtensionsAsCsv}",
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
                songIssues.Add(SongIssue.CreateWarning(songMeta, $"Video resource differs from audio resource. This is only supported for the formats {unitySupportedVideoFileExtensionsAsCsv}"));

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
        if (SongMetaUtils.GetWebsiteUri(songMeta).IsNullOrEmpty())
        {
            // Must have local audio file in supported format because no website is specified.
            CheckResourceExists(songIssues, songMeta, audioUri,
                () => $"Audio resource does not exist '{ApplicationUtils.ReplacePathsWithDisplayString(SongMetaUtils.GetAudioUri(songMeta))}'",
                ESongIssueSeverity.Error);
            CheckAudioOrVideoFormatIsSupported(songIssues, audioUri,
                () => $"Unsupported audio format '{GetUriOrExtensionWithoutDot(audioUri)}'. Convert to one of {unitySupportedAudioFileExtensionsAsCsv}",
                () => new FormatNotSupportedSongIssueData(songMeta, FormatNotSupportedSongIssueData.EMediaType.Audio),
                ESongIssueSeverity.Error);
        }
        // Check WebView URI is supported when specified
        else if (!WebViewUtils.CanHandleWebViewUrl(songMeta.Website))
        {
            if (!SongMetaUtils.LocalAudioResourceExists(songMeta))
            {
                // Cannot use the local audio file and not the website. This song cannot be played.
                songIssues.Add(SongIssue.CreateError(songMeta,
                    $"Audio resource does not exist '{ApplicationUtils.ReplacePathsWithDisplayString(SongMetaUtils.GetLocalAudioUri(songMeta))}' and website is not supported '{songMeta.Website}'. " +
                          $"Add the local audio file or provide JavaScript code to integrate the website in the embedded browser."));
            }
        }

        // Vocals audio and instrumental audio must use formats that are supported by Unity. Ffmpeg can only be used for the main audio.
        CheckResourceExists(songIssues, songMeta, songMeta.VocalsAudio,
            () => $"Vocals audio resource does not exist '{ApplicationUtils.ReplacePathsWithDisplayString(SongMetaUtils.GetVocalsAudioUri(songMeta))}'",
            ESongIssueSeverity.Warning);
        CheckIsUnitySupportedAudioFormat(songIssues, songMeta.VocalsAudio,
            () => $"Unsupported audio format '{GetUriOrExtensionWithoutDot(songMeta.VocalsAudio)}' for vocals . Convert to one of {unitySupportedAudioFileExtensionsAsCsv}",
            () => new FormatNotSupportedSongIssueData(songMeta, FormatNotSupportedSongIssueData.EMediaType.VocalsAudio),
            ESongIssueSeverity.Warning);
        CheckResourceExists(songIssues, songMeta, songMeta.InstrumentalAudio,
            () => $"Instrumental audio resource does not exist '{ApplicationUtils.ReplacePathsWithDisplayString(SongMetaUtils.GetInstrumentalAudioUri(songMeta))}'",
            ESongIssueSeverity.Warning);
        CheckIsUnitySupportedAudioFormat(songIssues, songMeta.InstrumentalAudio,
            () => $"Unsupported audio format '{GetUriOrExtensionWithoutDot(songMeta.InstrumentalAudio)}' for instrumental. Convert to one of {unitySupportedAudioFileExtensionsAsCsv}",
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
                codec => $"Unsupported video codec '{codec}' in '{songMeta.Audio}'. Convert to one of {unitySupportedVideoFileExtensionsAsCsv}",
                () => new FormatNotSupportedSongIssueData(songMeta, FormatNotSupportedSongIssueData.EMediaType.Video),
                ESongIssueSeverity.Error);
        }

        if (!songMeta.Video.IsNullOrEmpty())
        {
            CheckVideoCodecIsSupported(songIssues, songMeta, songMeta.Video,
                codec => $"Unsupported video codec '{codec}' in '{songMeta.Video}'. Convert to one of {unitySupportedVideoFileExtensionsAsCsv}",
                () => new FormatNotSupportedSongIssueData(songMeta, FormatNotSupportedSongIssueData.EMediaType.Video),
                ESongIssueSeverity.Warning);
        }
    }

    private static void CheckVideoCodecIsSupported(
        List<SongIssue> songIssues,
        SongMeta songMeta,
        string pathOrUri,
        Func<string, string> errorMessageGetter,
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
        Func<string> errorMessageGetter,
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
        Func<string> errorMessageGetter,
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
        Func<string> errorMessageGetter,
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
        Func<string> errorMessageGetter,
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
