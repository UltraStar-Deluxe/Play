using UnityEngine;
using UniInject;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

public class SongFileCacheLifeCycle : IOnLoadMod, IOnDisableMod
{
    [Inject]
    private SongFileCacheModSettings modSettings;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private ModObjectContext modObjectContext;

    private string CacheFilePath => $"{modObjectContext.ModPersistentDataFolder}/TxtFileCache.json";

    private SongCache loadedSongsCache;

    public void OnLoadMod()
    {
        Debug.Log($"{nameof(SongFileCacheLifeCycle)}.OnLoadMod");

        if (FileUtils.Exists(CacheFilePath))
        {
            // Load songs from cache
            loadedSongsCache = LoadCache(CacheFilePath);
            LoadSongsFromCacheObject(loadedSongsCache);
        }

        if (modSettings.songFolder.IsNullOrEmpty())
        {
            // Missing settings, thus abort.
            Debug.Log($"{nameof(SongFileCacheLifeCycle)} - Not caching *.txt files because no song folder specified in mod settings");
            return;
        }

        // Search txt files and update cache concurrently.
        Task.Run(() =>
        {
            List<string> txtFiles = SearchTxtFiles(modSettings.songFolder);
            UpdateSongsCache(txtFiles, CacheFilePath);
        });
    }

    private void LoadSongsFromCacheObject(SongCache songsCache)
    {
        Debug.Log($"{nameof(SongFileCacheLifeCycle)} - Loading {songsCache.CachedSongs.Count} songs from cache");
        foreach (CachedSong cachedSong in songsCache.CachedSongs)
        {
            try
            {
                LoadCachedSong(cachedSong);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to load cached song '{cachedSong.FilePath}', error: {ex.Message}");
            }
        }
    }

    private void LoadCachedSong(CachedSong cachedSong)
    {
        SongMeta songMeta;
        if (cachedSong.FileContent.IsNullOrEmpty())
        {
            // Lazy load file content on demand
            songMeta = new LazyLoadedFromFileSongMeta(cachedSong.FilePath);
        }
        else
        {
            // Load already cached file content now
            songMeta = UltraStarSongParser.ParseString(cachedSong.FileContent).SongMeta;
            songMeta.SetFileInfo(cachedSong.FilePath);
        }
        songMetaManager.AddSongMeta(songMeta);
    }

    private List<string> SearchTxtFiles(string songFolder)
    {
        if (!DirectoryUtils.Exists(songFolder))
        {
            Debug.Log($"{nameof(SongFileCacheLifeCycle)} - Not caching *.txt files because directory does not exist: {songFolder}");
            return new List<string>();
        }

        List<string> txtFiles = FileScanner.GetFiles(songFolder, new FileScannerConfig("*.txt") { Recursive = true } );
        Debug.Log($"{nameof(SongFileCacheLifeCycle)} - Found {txtFiles.Count} txt files in {songFolder}");

        return txtFiles;
    }

    private void UpdateSongsCache(List<string> txtFiles, string cacheFilePath)
    {
        SongCache newSongsCache = new SongCache(txtFiles, modSettings.cacheFileContent);

        if (loadedSongsCache != null
            && HasEqualSettings(loadedSongsCache, modSettings))
        {
            // Quick and dirty JSON comparison to determine whether songs changed
            string loadedSongsCacheJson = JsonConverter.ToJson(loadedSongsCache);
            string newSongsCacheJson = JsonConverter.ToJson(newSongsCache);
            if (loadedSongsCacheJson == newSongsCacheJson)
            {
                Debug.Log($"{nameof(SongFileCacheLifeCycle)} - Found songs are equal to last cached songs. Thus, not updating cache.");
                return;
            }
        }

        Debug.Log($"{nameof(SongFileCacheLifeCycle)} - Found songs are not equal to last cached songs. Thus, updating cache.");
        SaveCache(newSongsCache, cacheFilePath);

        // Notify user that songs cache changed
        if (newSongsCache.CachedSongs.Count > 0)
        {
            NotificationManager.CreateNotification(Translation.Of("UltraStar txt files changed.\nPlease restart the game to load them from cache."));
        }
    }

    private bool HasEqualSettings(SongCache loadedSongsCache, SongFileCacheModSettings modSettings)
    {
        return loadedSongsCache.CacheFileContent == modSettings.cacheFileContent;
    }

    private void SaveCache(SongCache songsCache, string cacheFilePath)
    {
        Debug.Log($"{nameof(SongFileCacheLifeCycle)} - saving songs to cache file: {cacheFilePath}");
        try
        {
            string json = JsonConverter.ToJson(songsCache);
            FileUtils.WriteAllText(cacheFilePath, json);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to save songs to cache file: {ex.Message}");
        }
    }

    private SongCache LoadCache(string cacheFilePath)
    {
        Debug.Log($"{nameof(SongFileCacheLifeCycle)} - loading songs from cache file: {cacheFilePath}");
        try
        {
            string json = FileUtils.ReadAllText(cacheFilePath);
            SongCache songsCache = JsonConverter.FromJson<SongCache>(json);
            return songsCache;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to load songs from cache file: {ex.Message}");
            return null;
        }
    }

    public void OnDisableMod()
    {
        Debug.Log($"{nameof(SongFileCacheLifeCycle)}.OnDisableMod");
    }
}

public class SongCache
{
    public List<CachedSong> CachedSongs { get; set; } = new List<CachedSong>();
    public bool CacheFileContent { get; set; }

    public SongCache()
    {
        // Empty constructor for JSON deserialization
    }

    public SongCache(IReadOnlyCollection<string> txtFiles, bool cacheFileContent)
    {
        CacheFileContent = cacheFileContent;
        foreach (string txtFile in txtFiles)
        {
            string fileContent = cacheFileContent
                ? FileUtils.ReadAllText(txtFile)
                : "";
            CachedSong songCache = new CachedSong(txtFile, fileContent);
            CachedSongs.Add(songCache);
        };
    }
}

public class CachedSong
{
    public string FilePath { get; set; }
    public string FileContent { get; set; }

    public CachedSong()
    {
        // Empty constructor for JSON deserialization
    }

    public CachedSong(string filePath, string fileContent)
    {
        FilePath = filePath;
        FileContent = fileContent;
    }
}