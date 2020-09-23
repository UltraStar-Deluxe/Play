using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using UniRx;
using UnityEngine;

// Handles loading and caching of SongMeta and related data structures (e.g. the voices are cached).
public class SongMetaManager : MonoBehaviour
{
    private static readonly object scanLock = new object();

    // The collection of songs is static to be persisted across scenes.
    // The collection is filled with song datas from a background thread, thus a thread-safe collection is used.
    private static ConcurrentBag<SongMeta> songMetas = new ConcurrentBag<SongMeta>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        songMetas = new ConcurrentBag<SongMeta>();
        isSongScanStarted = false;
        isSongScanFinished = false;
    }

    public static SongMetaManager Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<SongMetaManager>("SongMetaManager");
        }
    }

    private static bool isSongScanStarted;
    private static bool isSongScanFinished;
    public static bool IsSongScanFinished
    {
        get
        {
            return isSongScanFinished;
        }
    }

    private readonly Subject<SongScanFinishedEvent> songScanFinishedEventStream = new Subject<SongScanFinishedEvent>();
    public IObservable<SongScanFinishedEvent> SongScanFinishedEventStream => songScanFinishedEventStream;

    public int SongsFound { get; private set; }
    public int SongsSuccess { get; private set; }
    public int SongsFailed { get; private set; }

    public void Add(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            throw new ArgumentNullException("songMeta");
        }

        songMetas.Add(songMeta);
    }

    public SongMeta FindSongMeta(Func<SongMeta, bool> predicate)
    {
        SongMeta songMeta = songMetas.Where(predicate).FirstOrDefault();
        return songMeta;
    }

    public SongMeta GetFirstSongMeta()
    {
        return GetSongMetas().FirstOrDefault();
    }

    public IReadOnlyCollection<SongMeta> GetSongMetas()
    {
        return songMetas;
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
            SongsFound = 0;
            SongsSuccess = 0;
            SongsFailed = 0;

            FolderScanner scannerTxt = new FolderScanner("*.txt");

            // Find all txt files in the song directories
            txtFiles = ScanForTxtFiles(scannerTxt);
        }

        // Load the txt files in a background thread
        ThreadPool.QueueUserWorkItem(poolHandle =>
        {
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            Debug.Log("Started song-scan-thread.");
            lock (scanLock)
            {
                LoadTxtFiles(txtFiles);
                isSongScanFinished = true;
            }
            stopwatch.Stop();
            Debug.Log($"Finished song-scan-thread after {stopwatch.ElapsedMilliseconds} ms.");
            songScanFinishedEventStream.OnNext(new SongScanFinishedEvent());
        });
    }

    // Checks whether the audio and video file formats of the song are supported.
    // Returns true iff the audio file of the SongMeta exists and is supported.
    private bool CheckSupportedMediaFormats(SongMeta songMeta)
    {
        // Check video format.
        // Video is optional.
        if (!songMeta.Video.IsNullOrEmpty())
        {
            if (!ApplicationUtils.IsSupportedVideoFormat(Path.GetExtension(songMeta.Video)))
            {
                Debug.LogWarning("Unsupported video format: " + songMeta.Video);
                songMeta.Video = "";
            }
            else if (!File.Exists(SongMetaUtils.GetAbsoluteSongVideoPath(songMeta)))
            {
                Debug.LogWarning("Video file does not exist: " + SongMetaUtils.GetAbsoluteSongVideoPath(songMeta));
                songMeta.Video = "";
            }
        }

        // Check audio format.
        // Audio is mandatory. Without working audio file, the song cannot be played.
        if (!ApplicationUtils.IsSupportedAudioFormat(Path.GetExtension(songMeta.Mp3)))
        {
            Debug.LogWarning("Unsupported audio format: " + songMeta.Mp3);
            return false;
        }
        else if (!File.Exists(SongMetaUtils.GetAbsoluteSongFilePath(songMeta)))
        {
            Debug.LogWarning("Audio file does not exist: " + SongMetaUtils.GetAbsoluteSongFilePath(songMeta));
            return false;
        }

        return true;
    }

    private void LoadTxtFiles(List<string> txtFiles)
    {
        txtFiles.ForEach(delegate (string path)
        {
            try
            {
                SongMeta newSongMeta = SongMetaBuilder.ParseFile(path);
                if (CheckSupportedMediaFormats(newSongMeta))
                {
                    Add(newSongMeta);
                    SongsSuccess++;
                }
                else
                {
                    SongsFailed++;
                }
            }
            catch (SongMetaBuilderException e)
            {
                Debug.LogWarning(path + "\n" + e.Message);
                SongsFailed++;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError(path);
                SongsFailed++;
            }
        });
    }

    private List<string> ScanForTxtFiles(FolderScanner scannerTxt)
    {
        List<string> txtFiles = new List<string>();
        List<string> songDirs = SettingsManager.Instance.Settings.GameSettings.songDirs;
        foreach (string songDir in songDirs)
        {
            try
            {
                List<string> txtFilesInSongDir = scannerTxt.GetFiles(songDir);
                txtFiles.AddRange(txtFilesInSongDir);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        SongsFound = txtFiles.Count;
        Debug.Log($"Found {SongsFound} songs in {songDirs.Count} configured song directories");
        return txtFiles;
    }

    public int GetNumberOfSongsFound()
    {
        return SongsFound;
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

    public class SongScanFinishedEvent
    {
    }
}
