using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using UnityEngine;
using static ThreadPool;

// Handles loading and caching of SongMeta and related data structures (e.g. the voices are cached).
public class SongMetaManager : MonoBehaviour
{
    private static readonly object scanLock = new object();

    public int SongsFound { get; private set; }
    public int SongsSuccess { get; private set; }
    public int SongsFailed { get; private set; }

    public static SongMetaManager Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<SongMetaManager>("SongMetaManager");
        }
    }

    // The list of songs is static to be persisted across scenes.
    private static readonly List<SongMeta> songMetas = new List<SongMeta>();
    public List<SongMeta> SongMetas
    {
        get
        {
            if (songMetas.IsNullOrEmpty())
            {
                ScanFiles();
            }
            return songMetas;
        }
    }

    public void Add(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            throw new ArgumentNullException("songMeta");
        }
        lock (songMetas)
        {
            songMetas.Add(songMeta);
        }
    }

    public void Remove(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            throw new ArgumentNullException("songMeta");
        }
        lock (songMetas)
        {
            songMetas.Remove(songMeta);
        }
    }

    public SongMeta FindSongMeta(string songTitle)
    {
        List<SongMeta> songs = SongMetas;
        lock (songMetas)
        {
            SongMeta songMeta = songs.Find(it => it.Title == songTitle);
            return songMeta;
        }
    }

    public ReadOnlyCollection<SongMeta> GetSongMetas()
    {
        lock (songMetas)
        {
            return songMetas.AsReadOnly();
        }
    }

    public void ScanFiles()
    {
        Debug.Log("Scanning for UltraStar Songs");
        ScanFilesAsynchronously();
    }

    private void SortSongMetas()
    {
        // Sort by artist
        songMetas.Sort((songMeta1, songMeta2) => string.Compare(songMeta1.Artist, songMeta2.Artist, true, CultureInfo.InvariantCulture));
    }

    private void ScanFilesAsynchronously()
    {
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
            lock (scanLock)
            {
                LoadTxtFiles(txtFiles);
                SortSongMetas();
            }
        });
    }

    private void LoadTxtFiles(List<string> txtFiles)
    {
        txtFiles.ForEach(delegate (string path)
        {
            try
            {
                Add(SongMetaBuilder.ParseFile(path));
                SongsSuccess++;
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
}
