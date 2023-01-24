using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UniInject;
using UniRx;
using UnityEngine;

// Handles loading and caching of SongMeta and related data structures (e.g. the voices are cached).
public class SongMetaManager : AbstractSingletonBehaviour, INeedInjection
{
    private static readonly object scanLock = new();

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
        lastSongDirs = null;
    }

    public static SongMetaManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SongMetaManager>();

    // Static to be persisted across scenes.
    private static List<string> lastSongDirs;
    private static bool isSongScanStarted;
    private static bool isSongScanFinished;
    public static bool IsSongScanFinished
    {
        get
        {
            return isSongScanFinished;
        }
    }

    private readonly Subject<SongScanFinishedEvent> songScanFinishedEventStream = new();
    public IObservable<SongScanFinishedEvent> SongScanFinishedEventStream => songScanFinishedEventStream;

    [Inject]
    private Settings settings;

    public static void ResetSongMetas()
    {
        lock (scanLock)
        {
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
        ResetSongMetas();
        ScanFilesIfNotDoneYet();
    }

    protected override void StartSingleton()
    {
        RescanIfSongFoldersChanged();
    }

    private void RescanIfSongFoldersChanged()
    {
        if (lastSongDirs == null)
        {
            lastSongDirs = new List<string>(settings.GameSettings.songDirs);
        }

        if (isSongScanFinished
            && !lastSongDirs.SequenceEqual(settings.GameSettings.songDirs))
        {
            Debug.Log("SongDirs have changed since last scan. Start rescan.");
            lastSongDirs = new List<string>(settings.GameSettings.songDirs);
            ResetSongMetas();
            ScanFilesIfNotDoneYet();
        }
    }

    private void AddSongMeta(SongMeta songMeta)
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
                    ScanFilesAsynchronously();
                }
            }
        }
    }

    private void ScanFilesAsynchronously()
    {
        Debug.Log("ScanFilesAsynchronously");

        List<string> txtFiles;
        lock (scanLock)
        {
            FolderScanner txtScanner = new("*.txt");

            // Find all txt files in the song directories
            txtFiles = ScanForTxtFiles(txtScanner, settings.GameSettings.songDirs);
        }

        // Load the txt files in a background thread
        ThreadPool.QueueUserWorkItem(poolHandle =>
        {
            System.Diagnostics.Stopwatch stopwatch = new();
            stopwatch.Start();

            Debug.Log("Started song-scan-thread.");
            lock (scanLock)
            {
                LoadSongMetasFromTxtFiles(txtFiles, out List<SongMeta> newSongMetas, out List<SongIssue> newSongIssues);
                allSongMetas.AddRange(newSongMetas);
                allSongIssues.AddRange(newSongIssues);
                isSongScanFinished = true;
            }
            stopwatch.Stop();
            Debug.Log($"Finished song-scan-thread after {stopwatch.ElapsedMilliseconds} ms. Loaded {allSongMetas.Count} songs. Errors: {SongErrors.Count}, Warnings: {SongWarnings.Count}.");
            songScanFinishedEventStream.OnNext(new SongScanFinishedEvent());
        });
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

    private static List<string> ScanForTxtFiles(FolderScanner txtScanner, List<string> songDirs)
    {
        List<string> txtFiles = new();
        foreach (string songDir in songDirs)
        {
            try
            {
                List<string> txtFilesInSongDir = txtScanner.GetFiles(songDir, true);
                txtFiles.AddRange(txtFilesInSongDir);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        Debug.Log($"Found {allSongMetas.Count} songs in {songDirs.Count} configured song directories");
        return txtFiles;
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

        FolderScanner txtScanner = new("*.txt");
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
        songIssues = new List<SongIssue>();
        try
        {
            SongMeta newSongMeta = SongMetaBuilder.ParseFile(path, out List<SongIssue> parseFileIssues);
            songIssues.AddRange(parseFileIssues);

            List<SongIssue> mediaFormatIssues = SongMetaUtils.GetSupportedMediaFormatIssues(newSongMeta);
            songIssues.AddRange(mediaFormatIssues);

            if (songIssues.AllMatch(songIssue => songIssue.Severity == ESongIssueSeverity.Warning))
            {
                // No issues or only warnings, thus ok.
                songMeta = newSongMeta;
                return true;
            }
        }
        catch (SongMetaBuilderException e)
        {
            Debug.LogError("SongMetaBuilderException: " + path + "\n" + e.Message);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError(path);
        }

        songMeta = null;
        return false;
    }
}
