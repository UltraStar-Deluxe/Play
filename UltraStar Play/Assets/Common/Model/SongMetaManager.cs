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
public class SongMetaManager : MonoBehaviour, INeedInjection
{
    private static readonly object scanLock = new();

    // The collection of songs is static to be persisted across scenes.
    // The collection is filled with song datas from a background thread, thus a thread-safe collection is used.
    private static ConcurrentBag<SongMeta> songMetas = new();
    private static ConcurrentBag<SongIssue> songIssues = new();
    private static List<SongIssue> songErrors => songIssues.Where(songIssue => songIssue.Severity == ESongIssueSeverity.Error).ToList();
    private static List<SongIssue> songWarnings => songIssues.Where(songIssue => songIssue.Severity == ESongIssueSeverity.Warning).ToList();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        ResetSongMetas();
        lastSongDirs = null;
    }

    public static SongMetaManager Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<SongMetaManager>("SongMetaManager");
        }
    }

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
            songMetas = new ConcurrentBag<SongMeta>();
            songIssues = new ConcurrentBag<SongIssue>();
            isSongScanStarted = false;
            isSongScanFinished = false;
        }
    }

    public void ReloadSongMetas()
    {
        ResetSongMetas();
        ScanFilesIfNotDoneYet();
    }

    private void Start()
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

    public void Add(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            throw new ArgumentNullException(nameof(songMeta));
        }

        songMetas.Add(songMeta);
    }

    public SongMeta GetFirstSongMeta()
    {
        return GetSongMetas().FirstOrDefault();
    }

    public IReadOnlyCollection<SongMeta> GetSongMetas()
    {
        return songMetas;
    }

    public IReadOnlyList<SongIssue> GetSongIssues()
    {
        return songIssues.ToList();
    }

    public IReadOnlyList<SongIssue> GetSongErrors()
    {
        return songErrors;
    }

    public IReadOnlyList<SongIssue> GetSongWarnings()
    {
        return songWarnings;
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
            txtFiles = ScanForTxtFiles(txtScanner);
        }

        // Load the txt files in a background thread
        ThreadPool.QueueUserWorkItem(poolHandle =>
        {
            System.Diagnostics.Stopwatch stopwatch = new();
            stopwatch.Start();

            Debug.Log("Started song-scan-thread.");
            lock (scanLock)
            {
                LoadTxtFiles(txtFiles);
                isSongScanFinished = true;
            }
            stopwatch.Stop();
            Debug.Log($"Finished song-scan-thread after {stopwatch.ElapsedMilliseconds} ms. Loaded {songMetas.Count} songs. Errors: {songErrors.Count}, Warnings: {songWarnings.Count}.");
            songScanFinishedEventStream.OnNext(new SongScanFinishedEvent());
        });
    }

    private void LoadTxtFiles(List<string> txtFiles)
    {
        txtFiles.ForEach(path =>
        {
            try
            {
                SongMeta newSongMeta = SongMetaBuilder.ParseFile(path, out List<SongIssue> newSongIssues);
                newSongIssues.ForEach(songIssue => songIssues.Add(songIssue));

                newSongIssues = SongMetaUtils.GetSupportedMediaFormatIssues(newSongMeta);
                newSongIssues.ForEach(songIssue => songIssues.Add(songIssue));
                if (songIssues.AllMatch(songIssue => songIssue.Severity == ESongIssueSeverity.Warning))
                {
                    // No issues or only warnings. Can be added to song list.
                    Add(newSongMeta);
                }
            }
            catch (SongMetaBuilderException e)
            {
                string errorMessage = "SongMetaBuilderException: " + path + "\n" + e.Message;
                Debug.LogWarning(errorMessage);
                songIssues.Add(SongIssue.CreateError(null, errorMessage));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError(path);
                string errorMessage = "Exception: " + path + "\n" + e.Message;
                songIssues.Add(SongIssue.CreateError(null, errorMessage));
            }
        });
    }

    private List<string> ScanForTxtFiles(FolderScanner txtScanner)
    {
        List<string> txtFiles = new();
        List<string> songDirs = SettingsManager.Instance.Settings.GameSettings.songDirs;
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
        Debug.Log($"Found {songMetas.Count} songs in {songDirs.Count} configured song directories");
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

    public List<SongMeta> LoadNewSongMetasFromFolder(string songFolder)
    {
        List<SongMeta> result = new();
        if (!Directory.Exists(songFolder))
        {
            return result;
        }

        FolderScanner txtScanner = new("*.txt");
        List<string> txtFiles = txtScanner.GetFiles(songFolder, true);

        txtFiles.ForEach(path =>
        {
            try
            {
                SongMeta newSongMeta = SongMetaBuilder.ParseFile(path, out List<SongIssue> newSongIssues);
                newSongIssues.ForEach(songIssue => songIssues.Add(songIssue));

                newSongIssues = SongMetaUtils.GetSupportedMediaFormatIssues(newSongMeta);
                newSongIssues.ForEach(songIssue => songIssues.Add(songIssue));
                if (newSongIssues.AllMatch(songIssue => songIssue.Severity == ESongIssueSeverity.Warning))
                {
                    // No issues or only warnings. Can be added to song list.
                    result.Add(newSongMeta);
                }
            }
            catch (SongMetaBuilderException e)
            {
                Debug.LogWarning("SongMetaBuilderException: " + path + "\n" + e.Message);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError(path);
            }
        });

        result.ForEach(songMeta => Add(songMeta));
        return result;
    }

    public static void AddSongIssue(SongIssue songIssue)
    {
        songIssues.Add(songIssue);
    }
}
