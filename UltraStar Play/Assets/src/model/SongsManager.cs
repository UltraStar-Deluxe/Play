using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using UnityEngine;

public static class SongsManager
{
    private static readonly List<Song> s_songs = new List<Song>();
    private static string s_songScanStatus = "Waiting for song files scan to start.";

    public static void AddSongs(Song song)
    {
        if (song == null)
        {
            throw new UnityException("Can not add song because song is null!");
        }
        lock (s_songs)
        {
            s_songs.Add(song);
        }
    }

    public static void RemoveSong(Song song)
    {
        if (song == null)
        {
            throw new UnityException("Can not add song because song is null!");
        }
        lock (s_songs)
        {
            s_songs.Remove(song);
        }
    }

    public static ReadOnlyCollection<Song> GetSongs()
    {
        lock (s_songs)
        {
            return s_songs.AsReadOnly();
        }
    }

    public static void ScanSongFiles()
    {
        Thread thread = new Thread(new ThreadStart(ScanSongFilesAsThread));
        thread.Start();
    }

    private static void ScanSongFilesAsThread()
    {
        SetSongScanStatus("Initializing scan of song information files...");
        FolderScanner scannerTxt = new FolderScanner("*.txt");
        List<string> txtFiles = new List<string>();
        SetSongScanStatus("Scanning for matching file names in songs folder...");
        txtFiles = scannerTxt.GetFiles((string)SettingsManager.GetSetting(ESetting.SongDir));
        SetSongScanStatus(String.Format("Discovered a total of {0} possible song data files...", txtFiles.Count));
        int counter = 0;
        txtFiles.ForEach(delegate (string path)
        {
            counter += 1;
            SetSongScanStatus(String.Format("Scanning file {0} of {1} possible song data files...", counter, txtFiles.Count));
            try
            {
                if (SongParser.ParseSongFile(path))
                {
                    Debug.Log("success::" + path);
                }
                else
                {
                    Debug.Log("nope::" + path);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            SetSongScanStatus("Song files scan finished.");
        });
    }

    private static void SetSongScanStatus(string text)
    {
        lock(s_songScanStatus)
        {
            s_songScanStatus = text;
        }
    }

    public static string GetSongScanStatus()
    {
        lock(s_songScanStatus)
        {
            return s_songScanStatus;
        }
    }
}
