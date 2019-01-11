using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using UnityEngine;

public static class SongMetaManager
{
    private static readonly List<SongMeta> s_songMetas = new List<SongMeta>();
    private static object s_scanLock = new object();
    private static int s_songsFound;
    private static int s_songsSuccess;
    private static int s_songsFailed;

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
        Thread thread = new Thread(new ThreadStart(ScanFilesAsThread));
        thread.Start();
    }

    private static void ScanFilesAsThread()
    {
        lock (s_scanLock)
        {
            s_songsFound = 0;
            s_songsSuccess = 0;
            s_songsFailed = 0;
            FolderScanner scannerTxt = new FolderScanner("*.txt");
            List<string> txtFiles = scannerTxt.GetFiles((string)SettingsManager.GetSetting(ESetting.SongDir));
            s_songsFound = txtFiles.Count;
            txtFiles.ForEach(delegate (string path)
            {
                try
                {
                    Add(SongMetaBuilder.ParseFile(path));
                    Interlocked.Increment(ref s_songsSuccess);
                }
                catch (SongMetaBuilderException e)
                {
                    Debug.Log("nope::"+path+"\n"+e.Message);
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
