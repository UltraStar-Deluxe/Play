using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

public class SongIssueScanner
{
    private static string UnitySupportedVideoFileExtensionsCsv => ApplicationUtils.unitySupportedVideoFiles.JoinWith(", ");
    private static string UnitySupportedAudioFileExtensionsCsv => ApplicationUtils.unitySupportedAudioFiles.JoinWith(", ");

    public async Awaitable<List<SongIssue>> ScanSongIssuesAsync(
        Settings settings,
        IReadOnlyCollection<SongMeta> songMetas,
        CancellationToken cancellationToken,
        JobProgress jobProgress)
    {
        List<SongIssue> result = new();

        int doneSongMetas = 0;
        foreach (SongMeta songMeta in songMetas)
        {
            doneSongMetas++;
            jobProgress.EstimatedCurrentProgressInPercent = (double)doneSongMetas / songMetas.Count;

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

            // IDisposable d = new DisposableStopwatch($"Searching issues of song '{songMeta.GetArtistDashTitle()}' took <ms> ms");

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
                    settings.VlcToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Never);
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
        return result;
    }

    private static List<SongIssue> GetSongIssuesInSongFile(SongMeta songMeta)
    {
        List<SongIssue> result = new();

        if (songMeta.FileInfo != null
            && songMeta.FileInfo.Exists)
        {
            UltraStarSongParserResult parserResult = UltraStarSongParser.ParseFile(songMeta.FileInfo.FullName,
                new UltraStarSongParserConfig {Encoding = songMeta.FileEncoding, UseUniversalCharsetDetector = true, LogIssues = false});
            result.AddRange(parserResult.SongIssues);
        }

        return result;
    }

    /**
     * Checks whether the audio and video file formats of the song are supported.
     * Returns true iff the audio file of the SongMeta exists and is supported.
     */
    private static List<SongIssue> GetSupportedMediaFormatIssues(
        SongMeta songMeta,
        bool useVlcToPlayMediaFiles)
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

        // Vocals audio and instrumental audio must use formats that are supported by Unity.
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
