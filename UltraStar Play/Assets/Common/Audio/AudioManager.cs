using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

// Handles loading and caching of AudioClips.
// Use this over AudioUtils because AudioUtils does not cache AudioClips.
public class AudioManager : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        ClearCache();
    }

    private static readonly int criticalCacheSize = 10;
    private static readonly Dictionary<string, CachedAudioClip> audioClipCache = new();

    public static AudioManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<AudioManager>();

    private CoroutineManager coroutineManager;

    public AudioClip LoadAudioClipFromFile(string path, bool streamAudio = true)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("Audio file does not exist: " + path);
            return null;
        }

        return LoadAudioClipFromUri(path, streamAudio);
    }

    // When streamAudio is false, all audio data is loaded at once in a blocking way.
    public AudioClip LoadAudioClipFromUri(string uri, bool streamAudio = true)
    {
        if (audioClipCache.TryGetValue(uri, out CachedAudioClip cachedAudioClip)
            && (cachedAudioClip.StreamedAudioClip != null || cachedAudioClip.FullAudioClip))
        {
            if (streamAudio && cachedAudioClip.StreamedAudioClip != null)
            {
                return cachedAudioClip.StreamedAudioClip;
            }
            else if (!streamAudio && cachedAudioClip.FullAudioClip != null)
            {
                return cachedAudioClip.FullAudioClip;
            }
        }

        return LoadAndCacheAudioClip(uri, streamAudio);
    }

    public static void ClearCache()
    {
        foreach (CachedAudioClip cachedAudioClip in new List<CachedAudioClip>(audioClipCache.Values))
        {
            RemoveCachedAudioClip(cachedAudioClip);
        }
        audioClipCache.Clear();
    }

    private AudioClip LoadAndCacheAudioClip(string uri, bool streamAudio)
    {
        AudioClip audioClip = AudioUtils.GetAudioClipUncached(uri, streamAudio);
        if (audioClip == null)
        {
            Debug.LogError("Could not load AudioClip: " + uri);
            return null;
        }

        AddAudioClipToCache(uri, audioClip, streamAudio);
        return audioClip;
    }

    private static void AddAudioClipToCache(string path, AudioClip audioClip, bool streamAudio)
    {
        if (audioClipCache.Count >= criticalCacheSize)
        {
            RemoveOldestAudioClipsFromCache();
        }

        // Cache the new AudioClip.
        CachedAudioClip cachedAudioClip = new(path, audioClip, Time.frameCount, streamAudio);
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
            RemoveCachedAudioClip(oldest);
        }
    }

    private static void RemoveCachedAudioClip(CachedAudioClip cachedAudioClip)
    {
        audioClipCache.Remove(cachedAudioClip.Path);

        if (cachedAudioClip.StreamedAudioClip != null)
        {
            cachedAudioClip.StreamedAudioClip.UnloadAudioData();
        }

        if (cachedAudioClip.FullAudioClip != null)
        {
            cachedAudioClip.FullAudioClip.UnloadAudioData();
        }
    }

    private class LoadingAudioClip
    {
        public string Path { get; private set; }
        public DownloadHandlerAudioClip DownloadHandler { get; private set; }
        public List<Action<AudioClip>> Callbacks { get; private set; } = new();
        public long ElapsedMilliseconds
        {
            get
            {
                return stopwatch.ElapsedMilliseconds;
            }
        }

        private readonly System.Diagnostics.Stopwatch stopwatch;

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
        public AudioClip StreamedAudioClip { get; private set; }
        public AudioClip FullAudioClip { get; private set; }
        public int CreatedInFrame { get; private set; }

        public CachedAudioClip(string path, AudioClip audioClip, int currentFrame, bool isStreamedAudio)
        {
            Path = path;
            CreatedInFrame = currentFrame;
            if (isStreamedAudio)
            {
                StreamedAudioClip = audioClip;
            }
            else
            {
                FullAudioClip = audioClip;
            }
        }
    }
}
