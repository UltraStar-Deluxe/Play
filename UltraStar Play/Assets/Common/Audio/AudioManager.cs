using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Handles loading and caching of AudioClips.
// Use this over AudioUtils because AudioUtils does not cache AudioClips.
public static class AudioManager
{
    private static readonly int criticalCacheSize = 3;
    private static readonly Dictionary<string, CachedAudioClip> audioClipCache = new Dictionary<string, CachedAudioClip>();

    public static AudioClip GetAudioClip(string path)
    {
        if (!audioClipCache.TryGetValue(path, out CachedAudioClip cachedAudioClip))
        {
            return LoadAndCacheAudioClip(path);
        }
        if (cachedAudioClip.AudioClip.samples == 0 && cachedAudioClip.AudioClip.loadState == AudioDataLoadState.Loaded)
        {
            // The AudioClip was unloaded by Unity. It has to be reloaded.
            // It seems unloading audio data happens non-deterministically on scene changes.
            return LoadAndCacheAudioClip(path);
        }
        return cachedAudioClip.AudioClip;
    }

    private static AudioClip LoadAndCacheAudioClip(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("File does not exist: " + path);
            return null;
        }

        AudioClip audioClip = AudioUtils.GetAudioClip(path);
        if (audioClip == null)
        {
            Debug.LogError("Could load not AudioClip from path: " + path);
            return null;
        }

        if (audioClipCache.Count >= criticalCacheSize)
        {
            RemoveOldestAudioClipsFromCache();
        }

        // Cache the new AudioClip.
        CachedAudioClip cachedAudioClip = new CachedAudioClip(path, audioClip, Time.frameCount);
        audioClipCache[path] = cachedAudioClip;
        return audioClip;
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

    private class CachedAudioClip
    {
        public string Path { get; private set; }
        public AudioClip AudioClip { get; private set; }
        public int CreatedInFrame { get; private set; }

        public CachedAudioClip(string path, AudioClip audioClip, int currentFrame)
        {
            Path = path;
            AudioClip = audioClip;
            CreatedInFrame = currentFrame;
        }
    }

}
