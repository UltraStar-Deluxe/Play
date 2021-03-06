﻿using System;
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
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        volumeBeforeMute = -1;
        ClearCache();
    }

    private static readonly int criticalCacheSize = 10;
    private static readonly Dictionary<string, CachedAudioClip> audioClipCache = new Dictionary<string, CachedAudioClip>();

    private static float volumeBeforeMute = -1;

    public static AudioManager Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<AudioManager>("AudioManager");
        }
    }

    private CoroutineManager coroutineManager;

    public static void ToggleMuteAudio()
    {
        if (volumeBeforeMute >= 0)
        {
            AudioListener.volume = volumeBeforeMute;
            volumeBeforeMute = -1;
            UiManager.Instance.CreateNotification("Unmute");
        }
        else
        {
            volumeBeforeMute = AudioListener.volume;
            AudioListener.volume = 0;
            UiManager.Instance.CreateNotification("Mute");
        }
    }

    // When streamAudio is false, all audio data is loaded at once in a blocking way.
    public AudioClip LoadAudioClip(string path, bool streamAudio = true)
    {
        if (audioClipCache.TryGetValue(path, out CachedAudioClip cachedAudioClip)
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

        return LoadAndCacheAudioClip(path, streamAudio);
    }

    public void LoadAudioClipFromUri(string uri, Action<AudioClip> onSuccess, Action<UnityWebRequest> onFailure = null)
    {
        if (audioClipCache.TryGetValue(uri, out CachedAudioClip cachedAudioClip)
            && (cachedAudioClip.StreamedAudioClip != null || cachedAudioClip.FullAudioClip))
        {
            onSuccess(cachedAudioClip.FullAudioClip);
            return;
        }

        Action<AudioClip> doCacheAudioClipThenOnSuccess = (loadedAudioClip) =>
        {
            AddAudioClipToCache(uri, loadedAudioClip, true);
            onSuccess(loadedAudioClip);
        };

        if (coroutineManager == null)
        {
            coroutineManager = CoroutineManager.Instance;
        }
        coroutineManager.StartCoroutine(WebRequestUtils.LoadAudioClipFromUri(uri, doCacheAudioClipThenOnSuccess, onFailure));
    }

    public static void ClearCache()
    {
        foreach (CachedAudioClip cachedAudioClip in new List<CachedAudioClip>(audioClipCache.Values))
        {
            RemoveCachedAudioClip(cachedAudioClip);
        }
        audioClipCache.Clear();
    }

    private AudioClip LoadAndCacheAudioClip(string path, bool streamAudio)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("File does not exist: " + path);
            return null;
        }

        AudioClip audioClip = AudioUtils.GetAudioClipUncached(path, streamAudio);
        if (audioClip == null)
        {
            Debug.LogError("Could not load AudioClip from path: " + path);
            return null;
        }

        AddAudioClipToCache(path, audioClip, streamAudio);
        return audioClip;
    }

    private static void AddAudioClipToCache(string path, AudioClip audioClip, bool streamAudio)
    {
        if (audioClipCache.Count >= criticalCacheSize)
        {
            RemoveOldestAudioClipsFromCache();
        }

        // Cache the new AudioClip.
        CachedAudioClip cachedAudioClip = new CachedAudioClip(path, audioClip, Time.frameCount, streamAudio);
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
