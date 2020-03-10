using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

// Handles loading and caching of AudioClips.
// Use this over AudioUtils because AudioUtils does not cache AudioClips.
public class AudioManager : MonoBehaviour
{
    private const int LoadingTimeoutInMillis = 10000;

    private static readonly int criticalCacheSize = 10;
    private static readonly Dictionary<string, CachedAudioClip> audioClipCache = new Dictionary<string, CachedAudioClip>();

    private static List<LoadingAudioClip> loadingAudioClips = new List<LoadingAudioClip>();

    public static AudioManager Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<AudioManager>("AudioManager");
        }
    }

    void Update()
    {
        // Check if async loading of AudioClip has finished or timed out.
        List<LoadingAudioClip> doneLoadingAudioClips = new List<LoadingAudioClip>();
        foreach (LoadingAudioClip loadingAudioClip in loadingAudioClips)
        {
            if (loadingAudioClip.DownloadHandler.isDone)
            {
                AudioClip audioClip = loadingAudioClip.DownloadHandler.audioClip;
                loadingAudioClip.DisposeAndNotifyCallbacks(audioClip);
                doneLoadingAudioClips.Add(loadingAudioClip);
            }
            else if (loadingAudioClip.ElapsedMilliseconds > LoadingTimeoutInMillis)
            {
                Debug.LogError($"Loading AudioClip from path {loadingAudioClip.Path} timed out");
                loadingAudioClip.DisposeAndNotifyCallbacks(null);
                doneLoadingAudioClips.Add(loadingAudioClip);
            }
        }
        doneLoadingAudioClips.ForEach(it => loadingAudioClips.Remove(it));
    }

    public AudioClip GetAudioClip(string path, bool allowAsyncLoadedAudioClip = true)
    {
        if (audioClipCache.TryGetValue(path, out CachedAudioClip cachedAudioClip))
        {
            if (!allowAsyncLoadedAudioClip && cachedAudioClip.IsAsyncLoaded)
            {
                return LoadAndCacheAudioClip(path);
            }
            else
            {
                return cachedAudioClip.AudioClip;
            }
        }
        else
        {
            return LoadAndCacheAudioClip(path);
        }
    }

    public void GetAudioClipAsync(string path, Action<AudioClip> callback)
    {
        if (audioClipCache.TryGetValue(path, out CachedAudioClip cachedAudioClip))
        {
            callback(cachedAudioClip.AudioClip);
        }
        else
        {
            LoadAndCacheAudioClipAsync(path, callback);
        }
    }

    private AudioClip LoadAndCacheAudioClip(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("File does not exist: " + path);
            return null;
        }

        AudioClip audioClip = GetAudioClipUncached(path);
        if (audioClip == null)
        {
            Debug.LogError("Could not load not AudioClip from path: " + path);
            return null;
        }

        AddAudioClipToCache(path, audioClip, false);
        return audioClip;
    }

    private void LoadAndCacheAudioClipAsync(string path, Action<AudioClip> callback)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("File does not exist: " + path);
            callback(null);
            return;
        }

        GetAudioClipUnchachedAsync(path, loadedAudioClip =>
        {
            // Cache loaded AudioClip if loaded successfully.
            if (loadedAudioClip == null)
            {
                Debug.LogError("Could not load not AudioClip from path: " + path);
            }
            else
            {
                AddAudioClipToCache(path, loadedAudioClip, true);
            }
            // Call original callback
            callback(loadedAudioClip);
        });
    }

    // This method should only be called from tests and the AudioManager.
    // Use the cached version of the AudioManager for the normal game logic.
    public static AudioClip GetAudioClipUncached(string path)
    {
        if (!System.IO.File.Exists(path))
        {
            Debug.LogWarning($"Can not open audio file because it does not exist: {path}");
            return null;
        }
        string fileExtension = System.IO.Path.GetExtension(path);

        if (fileExtension.ToLowerInvariant().Equals(".mp3"))
        {
            AudioClip audioClip = AudioUtils.LoadMp3(path);
            return audioClip;
        }
        else
        {
            return LoadAudio(path);
        }
    }

    private static void GetAudioClipUnchachedAsync(string path, Action<AudioClip> callback)
    {
        if (!System.IO.File.Exists(path))
        {
            Debug.LogWarning($"Can not open audio file because it does not exist: {path}");
            callback(null);
            return;
        }
        string fileExtension = System.IO.Path.GetExtension(path);

        if (fileExtension.ToLowerInvariant().Equals(".mp3"))
        {
            AudioClip audioClip = AudioUtils.LoadMp3(path);
            callback(audioClip);
        }
        else
        {
            LoadAudioAsync(path, callback);
        }
    }

    private static AudioClip LoadAudio(string path)
    {
        using (UnityWebRequest webRequest = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.UNKNOWN))
        {
            webRequest.SendWebRequest();
            if (webRequest.isNetworkError || webRequest.isHttpError)
            {
                Debug.LogError("Error Loading Audio: " + path);
                Debug.LogError(webRequest.error);
                return null;
            }

            while (!webRequest.isDone)
            {
                Task.Delay(30);
            }
            return DownloadHandlerAudioClip.GetContent(webRequest);
        }
    }

    private static void LoadAudioAsync(string path, Action<AudioClip> callback)
    {
        LoadingAudioClip loadingAudioClip = loadingAudioClips.Where(it => it.Path == path).FirstOrDefault();
        if (loadingAudioClip != null)
        {
            // The audio clip is already beeing loaded.
            // Remember to notify the callback when loading is finished.
            loadingAudioClip.Callbacks.Add(callback);
            return;
        }

        UnityWebRequest webRequest = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.UNKNOWN);
        DownloadHandlerAudioClip downloadHandler = webRequest.downloadHandler as DownloadHandlerAudioClip;
        downloadHandler.streamAudio = true;

        webRequest.SendWebRequest();
        if (webRequest.isNetworkError || webRequest.isHttpError)
        {
            Debug.LogError("Error Loading Audio: " + path);
            Debug.LogError(webRequest.error);
            callback(null);
            return;
        }

        // Remember this on-going loading process and check in Update() for its completion.
        // Note that this only works if there is an instance of the AudioManager to call its Update() method.
        // Thus, loading AudioClip asynchronously does not work from tests!
        loadingAudioClips.Add(new LoadingAudioClip(path, downloadHandler, callback));
    }

    private static void AddAudioClipToCache(string path, AudioClip audioClip, bool isAsyncLoaded)
    {
        if (audioClipCache.Count >= criticalCacheSize)
        {
            RemoveOldestAudioClipsFromCache();
        }

        // Cache the new AudioClip.
        CachedAudioClip cachedAudioClip = new CachedAudioClip(path, audioClip, Time.frameCount, isAsyncLoaded);
        audioClipCache[path] = cachedAudioClip;
    }

    private static void RemoveOldestAudioClipsFromCache()
    {
        CachedAudioClip oldest = null;
        foreach (CachedAudioClip cachedAudioClip in audioClipCache.Values)
        {
            if (oldest == null || oldest.CreatedInFrame > cachedAudioClip.CreatedInFrame)
            {
                oldest = cachedAudioClip;
            }
        }

        if (oldest != null)
        {
            oldest.AudioClip.UnloadAudioData();
            audioClipCache.Remove(oldest.Path);
        }
    }

    private class LoadingAudioClip
    {
        public string Path { get; private set; }
        public DownloadHandlerAudioClip DownloadHandler { get; private set; }
        public List<Action<AudioClip>> Callbacks { get; private set; } = new List<Action<AudioClip>>();
        public long ElapsedMilliseconds
        {
            get
            {
                return stopwatch.ElapsedMilliseconds;
            }
        }

        private System.Diagnostics.Stopwatch stopwatch;

        public LoadingAudioClip(string path, DownloadHandlerAudioClip downloadHandler, Action<AudioClip> callback)
        {
            this.Path = path;
            this.DownloadHandler = downloadHandler;
            this.Callbacks.Add(callback);

            stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
        }

        public void DisposeAndNotifyCallbacks(AudioClip audioClip)
        {
            DownloadHandler.Dispose();
            foreach (Action<AudioClip> callback in Callbacks)
            {
                callback(audioClip);
            }
        }
    }

    private class CachedAudioClip
    {
        public string Path { get; private set; }
        public AudioClip AudioClip { get; private set; }
        public int CreatedInFrame { get; private set; }
        public bool IsAsyncLoaded { get; private set; }

        public CachedAudioClip(string path, AudioClip audioClip, int currentFrame, bool isAsyncLoaded)
        {
            Path = path;
            AudioClip = audioClip;
            CreatedInFrame = currentFrame;
            IsAsyncLoaded = isAsyncLoaded;
        }
    }
}
