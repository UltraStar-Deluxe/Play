using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using UnityEngine;

public class SongMetaManager : MonoBehaviour
{
    private static readonly List<SongMeta> s_songMetas = new List<SongMeta>();
    private static object s_scanLock = new object();
    private static int s_songsFound;
    private static int s_songsSuccess;
    private static int s_songsFailed;

    void OnEnable()
    {
        if (!SceneDataBus.HasData(ESceneData.AllSongMetas))
        {
            ScanFiles();
            SceneDataBus.PutData(ESceneData.AllSongMetas, s_songMetas);
        }
    }

    public static void Add(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            throw new ArgumentNullException("songMeta");
        }
        lock (s_songMetas)
        {
            s_songMetas.Add(songMeta);
        }
    }

    public static void Remove(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            throw new ArgumentNullException("songMeta");
        }
        lock (s_songMetas)
        {
            s_songMetas.Remove(songMeta);
        }
    }

    public static ReadOnlyCollection<SongMeta> GetSongMetas()
    {
        lock (s_songMetas)
        {
            return s_songMetas.AsReadOnly();
        }
    }

    public static void ScanFiles()
    {
        Debug.Log("Scanning for UltraStar Songs");
        ScanFilesSynchronously();
    }

    private static void ScanFilesSynchronously()
    {
        lock (s_scanLock)
        {
            s_songsFound = 0;
            s_songsSuccess = 0;
            s_songsFailed = 0;
            FolderScanner scannerTxt = new FolderScanner("*.txt");

            // Find all txt files in the song directories
            List<string> txtFiles = new List<string>();
            List<string> songDirs = SettingsManager.GetSetting(ESetting.SongDirs) as List<string>;
            foreach (var songDir in songDirs)
            {
                List<string> txtFilesInSongDir = scannerTxt.GetFiles(songDir);
                txtFiles.AddRange(txtFilesInSongDir);
            }
            s_songsFound = txtFiles.Count;
            Debug.Log($"Found {s_songsFound} songs in {songDirs.Count} configured song directories");

            txtFiles.ForEach(delegate (string path)
            {
                try
                {
                    Add(SongMetaBuilder.ParseFile(path));
                    Interlocked.Increment(ref s_songsSuccess);
                }
                catch (SongMetaBuilderException e)
                {
                    Debug.Log("nope::" + path + "\n" + e.Message);
                    Interlocked.Increment(ref s_songsFailed);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    Debug.Log("nope::" + path);
                    Interlocked.Increment(ref s_songsFailed);
                }
            });
        }
    }

    public static int GetNumberOfSongsFound()
    {
        return s_songsFound;
    }

    public static int GetNumberOfSongsScanned()
    {
        return s_songsSuccess + s_songsFailed;
    }

    public static int GetNumberOfSongsSuccess()
    {
        return s_songsSuccess;
    }

    public static int GetNumberOfSongsFailed()
    {
        return s_songsFailed;
    }
}
