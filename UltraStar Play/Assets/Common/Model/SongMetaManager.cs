using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using UnityEngine;

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

    private static readonly List<SongMeta> songMetas = new List<SongMeta>();
    public List<SongMeta> SongMetas
    {
        get
        {
            if (songMetas.Count == 0)
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
        SongMeta songMeta = SongMetas.Find(it => it.Title == songTitle);
        return songMeta;
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
        ScanFilesSynchronously();
    }

    private void ScanFilesSynchronously()
    {
        lock (scanLock)
        {
            SongsFound = 0;
            SongsSuccess = 0;
            SongsFailed = 0;
            FolderScanner scannerTxt = new FolderScanner("*.txt");

            // Find all txt files in the song directories
            List<string> txtFiles = new List<string>();
            List<string> songDirs = SettingsManager.Instance.Settings.GameSettings.songDirs;
            foreach (string songDir in songDirs)
            {
                List<string> txtFilesInSongDir = scannerTxt.GetFiles(songDir);
                txtFiles.AddRange(txtFilesInSongDir);
            }
            SongsFound = txtFiles.Count;
            Debug.Log($"Found {SongsFound} songs in {songDirs.Count} configured song directories");

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
    }

    public int GetNumberOfSongsFound()
    {
        return SongsFound;
    }
}
