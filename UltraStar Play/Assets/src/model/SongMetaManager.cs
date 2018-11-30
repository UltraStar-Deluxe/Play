using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using UnityEngine;

public static class SongMetaManager
{
    private static readonly List<SongMeta> s_songMetas = new List<SongMeta>();
    private static string s_scanStatus = "Waiting for song files scan to start.";

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
        SetScanStatus("Initializing scan of song information files...");
        FolderScanner scannerTxt = new FolderScanner("*.txt");
        List<string> txtFiles = new List<string>();
        SetScanStatus("Scanning for matching file names in songs folder...");
        txtFiles = scannerTxt.GetFiles((string)SettingsManager.GetSetting(ESetting.SongDir));
        SetScanStatus(String.Format("Discovered a total of {0} possible song data files...", txtFiles.Count));
        int counter = 0;
        txtFiles.ForEach(delegate (string path)
        {
            counter += 1;
            SetScanStatus(String.Format("Scanning file {0} of {1} possible song data files...", counter, txtFiles.Count));
            try
            {
                Add(SongMetaBuilder.ParseFile(path));
            }
            catch (SongMetaBuilderException e)
            {
                Debug.Log("nope::"+path+"\n"+e.Message);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.Log("nope::" + path);
            }
            SetScanStatus("Song files scan finished.");
        });
    }

    private static void SetScanStatus(string text)
    {
        lock(s_scanStatus)
        {
            s_scanStatus = text;
        }
    }

    public static string GetScanStatus()
    {
        lock(s_scanStatus)
        {
            return s_scanStatus;
        }
    }
}
