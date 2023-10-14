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
    private const int LazyLoadingSongRecommendationThresholdCount = 500;
    private static readonly object scanLock = new();

    // The collection of songs is static to be persisted across scenes.
    // The collection is filled with song datas from a background thread, thus a thread-safe collection is used.
    private static ConcurrentBag<SongMeta> allSongMetas = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        ResetSongMetas();
    }

    public static SongMetaManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SongMetaManager>();

    // Static to be persisted across scenes.
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

    [InjectedInAwake]
    private Settings settings;

    private List<string> EnabledSongFolders => SettingsUtils.GetEnabledSongFolders(settings);

    private static CancellationTokenSource songScanCancellationTokenSource;

    private static void ResetSongMetas()
    {
        lock (scanLock)
        {
            targetSongCount = 0;
            allSongMetas = new ConcurrentBag<SongMeta>();
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
        settings = SettingsManager.Instance.Settings;
    }

    public static void AddSongMeta(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return;
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
        Debug.Log("Starting song scan");

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

                // Show notification to the user when switching to lazy loading of songs is recommended.
                if (targetSongCount > LazyLoadingSongRecommendationThresholdCount
                    && settings.SongDataFetchType is EFetchType.Eager)
                {
                    UiManager.CreateNotification($"Configure on-demand loading\nof songs for faster setup.");
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    LoadAndAddSongMetasFromTxtFiles(txtFiles, cancellationToken);
                }

                // Only search for audio and midi files in configured song folders, not in the generated song folder
                if (settings.SearchMidiFilesWithLyrics)
                {
                    List<string> midiFiles = FileScannerUtils.ScanForFiles(EnabledSongFolders, GetMidiFileExtensionPatterns());
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        // Generate song meta for audio files that do not have a corresponding SongMeta.
                        GenerateSongMetasForAudioFiles(generatedSongFolderAbsolutePath, midiFiles, allSongMetas.ToList());
                    }
                }

                if (settings.SearchAudioFilesWithoutSongMeta)
                {
                    List<string> audioFiles = FileScannerUtils.ScanForFiles(EnabledSongFolders, GetNonMidiAudioFileExtensionPatterns());
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
            Debug.Log($"Finished song-scan-thread after {stopwatch.ElapsedMilliseconds} ms. Loaded {allSongMetas.Count} songs.");

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
            .Select(songMeta =>
            {
                string directoryPath = SongMetaUtils.GetDirectoryPath(songMeta);
                if (directoryPath.IsNullOrEmpty())
                {
                    return "";
                }
                return new DirectoryInfo(directoryPath).FullName;
            })
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

    private List<string> GetNonMidiAudioFileExtensionPatterns()
    {
        return ApplicationUtils.supportedAudioFiles
            .Except(ApplicationUtils.supportedMidiFiles)
            .Select(fileExtension => $"*.{fileExtension}")
            .ToList();
    }

    private List<string> GetMidiFileExtensionPatterns()
    {
        return ApplicationUtils.supportedMidiFiles
            .Select(fileExtension => $"*.{fileExtension}")
            .ToList();
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
            LazyLoadedFromFileSongMeta newSongMeta = new LazyLoadedFromFileSongMeta(path);
            if (settings.SongDataFetchType is EFetchType.Eager)
            {
                newSongMeta.LoadSongIfNotDoneYet();
            }
            songMeta = newSongMeta;
            return true;
        }
        catch (UltraStarSongParserException e)
        {
            Debug.LogError($"{nameof(UltraStarSongParserException)}: " + path + "\n" + e.Message);
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
        SongIdManager.ClearSongIds(songMeta);

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
        SongIdManager.ClearSongIds(songMeta);

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

    public SongMeta GetSongMetaByTitle(string title)
    {
        return allSongMetas.FirstOrDefault(songMeta => songMeta.Title == title);
    }

    public SongMeta GetSongMetaByGloballyUniqueId(string songId)
    {
        if (songId.IsNullOrEmpty())
        {
            return null;
        }

        if (SongIdManager.TryGetSongMetaByGloballyUniqueId(songId, out SongMeta knownMatchingSongMeta))
        {
            return knownMatchingSongMeta;
        }

        SongMeta matchingSongMeta = allSongMetas
            .FirstOrDefault(songMeta => SongIdManager.SongMetaMatchesGloballyUniqueSongId(songMeta, songId));
        return matchingSongMeta;
    }

    public SongMeta GetSongMetaByLocallyUniqueId(string songId)
    {
        if (songId.IsNullOrEmpty())
        {
            return null;
        }

        if (SongIdManager.TryGetSongMetaByLocallyUniqueId(songId, out SongMeta knownMatchingSongMeta))
        {
            return knownMatchingSongMeta;
        }

        SongMeta matchingSongMeta = allSongMetas
            .FirstOrDefault(songMeta => SongIdManager.SongMetaMatchesLocallyUniqueSongId(songMeta, songId));
        return matchingSongMeta;
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

    public bool ContainsSongMeta(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return false;
        }

        return allSongMetas.Contains(songMeta);
    }
}
