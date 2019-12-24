using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using UnityEngine;

// Handles loading and caching of SongMeta and related data structures (e.g. the voices are cached).
public class SongMetaManager : MonoBehaviour
{
    private static readonly object scanLock = new object();

    public int SongsFound { get; private set; }
    public int SongsSuccess { get; private set; }
    public int SongsFailed { get; private set; }

    private static Dictionary<string, CachedVoices> voicesCache = new Dictionary<string, CachedVoices>();

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

    public static Dictionary<string, Voice> GetVoices(SongMeta songMeta)
    {
        string path = songMeta.Directory + Path.DirectorySeparatorChar + songMeta.Filename;
        if (!voicesCache.TryGetValue(path, out CachedVoices cachedVoices))
        {
            using (new DisposableStopwatch($"Loading voices of {path} took <millis> ms"))
            {
                Dictionary<string, Voice> voiceIdentifierToVoiceMap = VoicesBuilder.ParseFile(path, songMeta.Encoding, new List<string>());
                cachedVoices = new CachedVoices(path, voiceIdentifierToVoiceMap);
                voicesCache.Add(path, cachedVoices);
            }
        }
        return cachedVoices.VoiceIdentifierToVoiceMap;
    }

    public void ScanFiles()
    {
        Debug.Log("Scanning for UltraStar Songs");
        ScanFilesSynchronously();
        SortSongMetas();
    }

    private void SortSongMetas()
    {
        // Sort by artist
        songMetas.Sort((songMeta1, songMeta2) => string.Compare(songMeta1.Artist, songMeta2.Artist, true));
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

    private class CachedVoices
    {
        public string SongMetaFilePath { get; private set; }
        public Dictionary<string, Voice> VoiceIdentifierToVoiceMap { get; private set; }

        public CachedVoices(string songMetaFilePath, Dictionary<string, Voice> voiceIdentifierToVoiceMap)
        {
            this.SongMetaFilePath = songMetaFilePath;
            this.VoiceIdentifierToVoiceMap = voiceIdentifierToVoiceMap;
        }
    }
}
