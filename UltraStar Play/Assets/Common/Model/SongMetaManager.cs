using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Serilog.Events;
using UniRx;
using UnityEngine;
using Debug = UnityEngine.Debug;

// Handles loading and caching of SongMeta and related data structures (e.g. the voices are cached).
public class SongMetaManager : AbstractSingletonBehaviour
{
    private static readonly object scanLock = new();

    private static string unitySupportedVideoFileExtensionsAsCsv = ApplicationUtils.unitySupportedVideoFiles.ToCsv(",", "", "");
    private static string unitySupportedAudioFileExtensionsAsCsv = ApplicationUtils.unitySupportedAudioFiles.ToCsv(",", "", "");

    // The collection of songs is static to be persisted across scenes.
    // The collection is filled with song datas from a background thread, thus a thread-safe collection is used.
    private static ConcurrentBag<SongMeta> allSongMetas = new();
    private static ConcurrentBag<SongIssue> allSongIssues = new();
    private static List<SongIssue> SongErrors => allSongIssues.Where(songIssue => songIssue.Severity == ESongIssueSeverity.Error).ToList();
    private static List<SongIssue> SongWarnings => allSongIssues.Where(songIssue => songIssue.Severity == ESongIssueSeverity.Warning).ToList();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        ResetSongMetas();
        lastEnabledSongFolders = null;
    }

    public static SongMetaManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SongMetaManager>();

    private static readonly Dictionary<SongMeta, string> songMetaToScoreRelevantHash = new();
    private static readonly Dictionary<SongMeta, string> songMetaToUniqueHash = new();

    // Static to be persisted across scenes.
    private static List<string> lastEnabledSongFolders;
    private static bool isSongScanStarted;
    private static bool isSongScanFinished;
    public static bool IsSongScanFinished => isSongScanFinished;

    private static int targetSongCount;
    public static int LoadedSongsCount => allSongMetas.Count;
    public static double LoadedSongsPercent
    {
        get
        {
            if (targetSongCount <= 0)
            {
                return 0;
            }

            int result = 100 * allSongMetas.Count / targetSongCount;
            if (result >= 100 && !isSongScanFinished)
            {
                return 99.9;
            }
            return result;
        }
    }

    private readonly Subject<SongScanFinishedEvent> songScanFinishedEventStream = new();
    public IObservable<SongScanFinishedEvent> SongScanFinishedEventStream => songScanFinishedEventStream;

    private UiManager uiManager;
    private Settings settings;

    private List<string> EnabledSongFolders => SettingsUtils.GetEnabledSongFolders(settings);

    private static CancellationTokenSource songScanCancellationTokenSource;

    public static void ResetSongMetas()
    {
        lock (scanLock)
        {
            targetSongCount = 0;
            allSongMetas = new ConcurrentBag<SongMeta>();
            allSongIssues = new ConcurrentBag<SongIssue>();
            isSongScanStarted = false;
            isSongScanFinished = false;
        }
    }

    protected override object GetInstance()
    {
        return Instance;
    }

    public void ReloadSongMetas()
    {
        string generatedSongFolderAbsolutePath = SettingsUtils.GetGeneratedSongFolderAbsolutePath(settings);
        ThreadPool.QueueUserWorkItem(_ =>
        {
            CancelSongScanIfRunning();

            lock (scanLock)
            {
                ResetSongMetas();
                DoScanFilesIfNotDoneYet(generatedSongFolderAbsolutePath);
            }
        });
    }

    protected override void AwakeSingleton()
    {
        uiManager = UiManager.Instance;
        settings = SettingsManager.Instance.Settings;
    }

    protected override void StartSingleton()
    {
        RescanIfSongFoldersChanged();
    }

    private void RescanIfSongFoldersChanged()
    {
        // Scene injection may not have finished here because DefaultSceneDataProviders may trigger a song scan.
        // Thus, use the static instance.
        if (lastEnabledSongFolders == null)
        {
            lastEnabledSongFolders = new List<string>(EnabledSongFolders);
        }

        if (isSongScanFinished
            && !lastEnabledSongFolders.SequenceEqual(EnabledSongFolders))
        {
            Debug.Log("SongDirs have changed since last scan. Start rescan.");
            lastEnabledSongFolders = new List<string>(EnabledSongFolders);
            ResetSongMetas();
            ScanFilesIfNotDoneYet();
        }
    }

    public void AddSongMeta(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            throw new ArgumentNullException(nameof(songMeta));
        }

        allSongMetas.Add(songMeta);
    }

    public SongMeta GetFirstSongMeta()
    {
        return GetSongMetas().FirstOrDefault();
    }

    public IReadOnlyCollection<SongMeta> GetSongMetas()
    {
        return allSongMetas;
    }

    public IReadOnlyList<SongIssue> GetSongIssues()
    {
        return allSongIssues.ToList();
    }

    public IReadOnlyList<SongIssue> GetSongErrors()
    {
        return SongErrors;
    }

    public IReadOnlyList<SongIssue> GetSongWarnings()
    {
        return SongWarnings;
    }

    public void ScanFilesIfNotDoneYet()
    {
        DoScanFilesIfNotDoneYet(SettingsUtils.GetGeneratedSongFolderAbsolutePath(settings));
    }

    private void DoScanFilesIfNotDoneYet(string generatedSongFolderAbsolutePath)
    {
        // First check. If the songs have been scanned already,
        // then this will quickly return and allows multiple threads access.
        if (!isSongScanStarted)
        {
            // The songs have not been scanned. Only one thread must perform the scan action.
            lock (scanLock)
            {
                // From here on, reading and writing the isInitialized flag can be considered atomic.
                // Second check. If multiple threads attempted to scan for songs (they passed the first check),
                // then only the first of these threads will start the scan.
                if (!isSongScanStarted)
                {
                    isSongScanStarted = true;
                    isSongScanFinished = false;
                    songScanCancellationTokenSource?.Cancel();
                    songScanCancellationTokenSource = new();
                    ScanFilesAsynchronously(generatedSongFolderAbsolutePath, songScanCancellationTokenSource.Token);
                }
            }
        }
    }

    private void ScanFilesAsynchronously(string generatedSongFolderAbsolutePath, CancellationToken cancellationToken)
    {
        Debug.Log("ScanFilesAsynchronously");

        // Update supported file formats when ffmpeg is (not) used.
        ApplicationUtils.UseFfmpegToPlayMediaFiles = settings.FfmpegToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Never;
        ApplicationUtils.UseVlcToPlayMediaFiles = settings.VlcToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Never;

        // Scene injection may not have finished here because DefaultSceneDataProviders may trigger a song scan.
        // Thus, use the static instance.
        InitFolderIfNotDoneYet(generatedSongFolderAbsolutePath);

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        // Load the txt files in a background thread
        ThreadPool.QueueUserWorkItem(poolHandle =>
        {
            Stopwatch stopwatch = new();
            stopwatch.Start();

            Debug.Log("Started song-scan-thread.");

            lock (scanLock)
            {
                // Find all txt and audio files in configured song folders and the generated song folder
                List<string> allSongFolders = EnabledSongFolders
                    .Union(new List<string> { generatedSongFolderAbsolutePath })
                    .ToList();
                List<string> txtFiles = FileScannerUtils.ScanForFiles(allSongFolders, new List<string> { "*.txt" });
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                targetSongCount += txtFiles.Count;

                if (!cancellationToken.IsCancellationRequested)
                {
                    LoadAndAddSongMetasFromTxtFiles(txtFiles, cancellationToken);
                }

                // Only search for audio files in configured song folders, not in the generated song folder
                if (settings.SearchAudioFilesWithoutSongMeta)
                {
                    List<string> audioFiles = FileScannerUtils.ScanForFiles(EnabledSongFolders, GetAudioFileExtensionPatterns());
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        // Generate song meta for audio files that do not have a corresponding SongMeta.
                        GenerateSongMetasForAudioFiles(generatedSongFolderAbsolutePath, audioFiles, allSongMetas.ToList());
                    }
                }

                isSongScanFinished = true;
            }

            stopwatch.Stop();
            Debug.Log($"Finished song-scan-thread after {stopwatch.ElapsedMilliseconds} ms. Loaded {allSongMetas.Count} songs. Errors: {SongErrors.Count}, Warnings: {SongWarnings.Count}.");

            songScanFinishedEventStream.OnNext(new SongScanFinishedEvent(allSongMetas.Count));
        });
    }

    private void GenerateSongMetasForAudioFiles(
        string generatedSongFolderAbsolutePath,
        List<string> audioFiles,
        List<SongMeta> existingSongMetas)
    {
        if (audioFiles.IsNullOrEmpty())
        {
            return;
        }

        // Exclude audio files that are used in UltraStar txt files
        List<string> existingSongMetaAudioFiles = existingSongMetas
            .SelectMany(songMeta => GetAbsoluteAudioFilePaths(songMeta))
            .ToList();

        // Exclude audio files that are stored next to an UltraStar txt file
        HashSet<string> existingSongMetaFolders = existingSongMetas
            .Select(songMeta => new DirectoryInfo(SongMetaUtils.GetDirectoryPath(songMeta)).FullName)
            .ToHashSet();

        List<string> audioFilesWithoutSongMeta = audioFiles
            .Where(audioFile =>
            {
                bool isGeneratedAudioFile = ApplicationUtils.IsGeneratedAudioFile(audioFile);
                bool isNextToExistingSongMeta = existingSongMetaFolders.Contains(new FileInfo(audioFile).Directory.FullName);
                return !isGeneratedAudioFile && !isNextToExistingSongMeta;
            })
            .Select(audioFile => PathUtils.NormalizePath(Path.GetFullPath(audioFile)))
            .Except(existingSongMetaAudioFiles)
            .ToList();

        Debug.Log($"Found {audioFilesWithoutSongMeta.Count} audio files without corresponding SongMeta");

        List<SongMeta> generatedSongMetas = audioFilesWithoutSongMeta
            .Select(audioFile => GenerateSongMetaForAudioFile(generatedSongFolderAbsolutePath, audioFile))
            .Where(generatedSongMeta => generatedSongMeta != null)
            .ToList();

        generatedSongMetas.ForEach(songMeta => allSongMetas.Add(songMeta));
    }

    private List<string> GetAbsoluteAudioFilePaths(SongMeta songMeta)
    {
        List<string> result = new List<string>();

        void TryAddAudioFilePath(string audioFilePath)
        {
            if (SongMetaUtils.ResourceExists(songMeta, audioFilePath))
            {
                string absoluteFilePath = SongMetaUtils.GetAbsoluteFilePath(songMeta, audioFilePath);
                result.Add(PathUtils.NormalizePath(absoluteFilePath));
            }
        }

        TryAddAudioFilePath(songMeta.Audio);
        TryAddAudioFilePath(songMeta.VocalsAudio);
        TryAddAudioFilePath(songMeta.InstrumentalAudio);

        return result
            .Distinct()
            .ToList();
    }

    private SongMeta GenerateSongMetaForAudioFile(string generatedSongFolderAbsolutePath, string audioFile)
    {
        if (!AudioFileMetaTagUtils.TryGetArtist(audioFile, out string artist))
        {
            artist = "";
        }
        if (!AudioFileMetaTagUtils.TryGetTitle(audioFile, out string title))
        {
            title = Path.GetFileNameWithoutExtension(audioFile);
        }

        // TODO: use https://github.com/WestHillApps/UniBpmAnalyzer to analyze bpm
        // TODO: use https://github.com/Zeugma440/atldotnet to read meta tags.
        float txtFileBpm = 300;

        Dictionary<EVoiceId, string> voiceIdToDisplayName = new();

        SongMeta songMeta;

        string fileExtension = Path.GetExtension(new Uri(audioFile).LocalPath);
        if (ApplicationUtils.IsSupportedMidiFormat(fileExtension))
        {
            // Load lyrics and notes from MIDI file
            songMeta = new MidiFileSongMeta(
                artist,
                title,
                txtFileBpm,
                audioFile,
                voiceIdToDisplayName);
        }
        else
        {
            songMeta = new UltraStarSongMeta(
                artist,
                title,
                txtFileBpm,
                audioFile,
                voiceIdToDisplayName);
        }

        string absoluteSongMetaFilePath = GetAbsoluteGeneratedSongMetaFilePathForAudioFile(generatedSongFolderAbsolutePath, audioFile);
        songMeta.SetFileInfo(absoluteSongMetaFilePath);


        Debug.Log("Generated SongMeta: " + songMeta);
        return songMeta;
    }

    public static string GetAbsoluteGeneratedSongMetaFilePathForAudioFile(string generatedSongFolderAbsolutePath, string audioFile)
    {
        return ApplicationUtils.GetGeneratedOutputFolderForSourceFilePath(generatedSongFolderAbsolutePath, audioFile) + "/song-info.txt";
    }

    private List<string> GetAudioFileExtensionPatterns()
    {
        if (settings.SearchAudioFilesWithoutSongMeta)
        {
            return ApplicationUtils.supportedAudioFiles
                .Select(fileExtension => $"*.{fileExtension}")
                .ToList();
        }

        // Only search MIDI files with lyrics
        return new List<string> { "*.mid", "*.kar" };
    }

    private void InitFolderIfNotDoneYet(string path)
    {
        if (!Directory.Exists(path))
        {
            Debug.Log("Creating folder: " + path);
            Directory.CreateDirectory(path);
        }
    }

    private void LoadAndAddSongMetasFromTxtFiles(List<string> txtFiles, CancellationToken cancellationToken)
    {
        foreach (string path in txtFiles)
        {
            if (TryLoadSongMetaFromFile(path, out SongMeta newSongMeta, out List<SongIssue> newSongIssues))
            {
                allSongMetas.Add(newSongMeta);
            }
            allSongIssues.AddRange(newSongIssues);

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private void LoadSongMetasFromTxtFiles(List<string> txtFiles, out List<SongMeta> songMetas, out List<SongIssue> songIssues)
    {
        songMetas = new();
        songIssues = new();
        foreach (string path in txtFiles)
        {
            if (TryLoadSongMetaFromFile(path, out SongMeta newSongMeta, out List<SongIssue> newSongIssues))
            {
                songMetas.Add(newSongMeta);
            }
            songIssues.AddRange(newSongIssues);
        }
    }

    public void WaitUntilSongScanFinished()
    {
        ScanFilesIfNotDoneYet();
        float startTimeInSeconds = Time.time;
        float timeoutInSeconds = 2;
        while ((startTimeInSeconds + timeoutInSeconds) > Time.time)
        {
            if (isSongScanFinished)
            {
                return;
            }
            Thread.Sleep(100);
        }
        Debug.LogError("Song scan did not finish - timeout reached.");
    }

    public bool TryLoadAndAddSongMetasFromFolder(string songFolder, out List<SongMeta> songMetas, out List<SongIssue> songIssues)
    {
        songMetas = new List<SongMeta>();
        songIssues = new List<SongIssue>();
        if (!Directory.Exists(songFolder))
        {
            return false;
        }

        FileScanner txtScanner = new("*.txt", true, true);
        List<string> txtFiles = txtScanner.GetFiles(songFolder, true);

        LoadSongMetasFromTxtFiles(txtFiles, out List<SongMeta> newSongMetas, out List<SongIssue> newSongIssues);
        allSongMetas.AddRange(newSongMetas);
        allSongIssues.AddRange(newSongIssues);

        songMetas.AddRange(newSongMetas);
        newSongIssues.AddRange(newSongIssues);

        return true;
    }

    private bool TryLoadSongMetaFromFile(string path, out SongMeta songMeta, out List<SongIssue> songIssues)
    {
        string fileName = Path.GetFileName(path);
        List<string> ignoredFileNames = new() { "license.txt" };
        if (ignoredFileNames.AnyMatch(ignoredFileName => string.Equals(fileName, ignoredFileName)))
        {
            songMeta = null;
            songIssues = new List<SongIssue>();
            return false;
        }

        songIssues = new List<SongIssue>();
        try
        {
            SongMeta newSongMeta = UltraStarSongParser.ParseFile(path, out List<SongIssue> parseFileIssues, null, settings.UseUniversalCharsetDetector);
            songIssues.AddRange(parseFileIssues);

            List<SongIssue> mediaFormatIssues = GetSupportedMediaFormatIssues(
                newSongMeta,
                settings.FfmpegToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Never,
                settings.VlcToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Never,
                settings.CheckCodecIsSupported);
            songIssues.AddRange(mediaFormatIssues);

            if (songIssues.AllMatch(songIssue => songIssue.Severity == ESongIssueSeverity.Warning))
            {
                // No issues or only warnings, thus ok.
                songMeta = newSongMeta;
                return true;
            }
        }
        catch (UltraStarSongParserException e)
        {
            Debug.LogError("SongMetaBuilderException: " + path + "\n" + e.Message);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to load {path}");
        }

        songMeta = null;
        return false;
    }

    public void SaveSong(SongMeta songMeta, bool isAutoSave)
    {
        songMetaToScoreRelevantHash.Remove(songMeta);
        songMetaToUniqueHash.Remove(songMeta);

        SongMetaUtils.CreateDirectory(songMeta);
        string songFilePath = SongMetaUtils.GetAbsoluteSongMetaFilePath(songMeta);
        try
        {
            // Write the song data structure to the file.
            Debug.Log($"Saving song {songFilePath}");
            UltraStarFormatWriter.WriteFile(songFilePath, songMeta);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            UiManager.CreateNotification("Saving the file failed:\n" + e.Message);
            return;
        }

        if (!isAutoSave)
        {
            UiManager.CreateNotification("Saved file");
        }
    }

    public void ReloadSong(SongMeta songMeta)
    {
        string absoluteFilePath = SongMetaUtils.GetAbsoluteSongMetaFilePath(songMeta);
        try
        {
            SongMeta other = UltraStarSongParser.ParseFile(absoluteFilePath, out List<SongIssue> _, songMeta.FileEncoding, false);
            songMeta.CopyValues(other);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to reload song {absoluteFilePath}: " + e.Message);
            Debug.LogException(e);
        }
    }

    public SongMeta GetSongMetaById(string songId)
    {
        if (songId.IsNullOrEmpty())
        {
            return null;
        }
        SongMeta matchingSongMeta = allSongMetas.FirstOrDefault(songMeta =>
            GetAndCacheUniqueHash(songMeta) == songId);
        return matchingSongMeta;
    }

    public List<SongMeta> GetSongMetasByIds(List<string> songIds)
    {
        if (songIds.IsNullOrEmpty())
        {
            return new();
        }

        return songIds
            .Select(id => GetSongMetaById(id))
            .ToList();
    }

    public SongMeta GetSongMetaByTitle(string title)
    {
        return allSongMetas.FirstOrDefault(songMeta => songMeta.Title == title);
    }

    // Checks whether the audio and video file formats of the song are supported.
    // Returns true iff the audio file of the SongMeta exists and is supported.
    public static List<SongIssue> GetSupportedMediaFormatIssues(
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

    public static string GetAndCacheScoreRelevantHash(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return "";
        }

        if (songMetaToScoreRelevantHash.TryGetValue(songMeta, out string scoreRelevantHash))
        {
            return scoreRelevantHash;
        }

        scoreRelevantHash = SongMetaUtils.ComputeScoreRelevantSongHash(songMeta);
        songMetaToScoreRelevantHash[songMeta] = scoreRelevantHash;
        return scoreRelevantHash;
    }

    public static string GetAndCacheUniqueHash(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return "";
        }

        if (songMetaToUniqueHash.TryGetValue(songMeta, out string hash))
        {
            return hash;
        }

        hash = SongMetaUtils.ComputeUniqueSongHash(songMeta);
        songMetaToUniqueHash[songMeta] = hash;
        return hash;
    }

    protected override void OnDestroySingleton()
    {
        CancelSongScanIfRunning();
    }

    private void CancelSongScanIfRunning()
    {
        if (isSongScanStarted
            && !isSongScanFinished
            && songScanCancellationTokenSource != null)
        {
            Debug.Log($"Cancelling song-scan-thread.");
            songScanCancellationTokenSource.Cancel();
        }
    }
}
