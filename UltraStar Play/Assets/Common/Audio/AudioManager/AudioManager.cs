using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;

/**
 * Handles loading and caching of AudioClips.
 */
public class AudioManager : AbstractSingletonBehaviour, INeedInjection
{
    public static AudioManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<AudioManager>();

    private const int CriticalCacheSize = 10;
    private readonly Dictionary<string, CachedAudioClip> audioClipCache = new();

    [InjectedInInspector]
    public AudioMixerGroup pitchShifterAudioMixerGroup;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void OnDestroySingleton()
    {
        ClearCache();
    }

    public static async Awaitable<AudioClip> LoadAudioClipFromUriAsync(string uri, bool streamAudio = true)
    {
        return await Instance.DoLoadAudioClipFromUriAsync(uri, streamAudio);
    }

    private async Awaitable<AudioClip> DoLoadAudioClipFromUriAsync(string uri, bool streamAudio)
    {
        if (uri.IsNullOrEmpty())
        {
            throw new LoadAudioException("Cannot load AudioClip, URI is null or empty");
        }

        if (!ApplicationUtils.IsUnitySupportedAudioFormat(Path.GetExtension(uri)))
        {
            throw new LoadAudioException($"Cannot load AudioClip because the format is not supported by Unity. URI: '{uri}', supported formats: {ApplicationUtils.unitySupportedAudioFiles.JoinWith(", ")}");
        }

        if (!TryGetUri(uri, out Uri uriObject))
        {
            throw new LoadAudioException($"URI is invalid. Maybe the file does not exist. URI: '{uri}'");
        }

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

        try
        {
            using UnityWebRequest webRequest = CreateAudioClipRequest(uriObject, streamAudio);
            await WebRequestUtils.SendWebRequestAsync(webRequest);

            AudioClip audioClip = (webRequest.downloadHandler as DownloadHandlerAudioClip).audioClip;
            AddAudioClipToCache(uri, audioClip, streamAudio);
            return audioClip;
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to load AudioClip from URI: '{uri}': {ex.Message}");
            throw ex;
        }
    }

    private bool TryGetUri(string uriString, out Uri uri)
    {
        try
        {
            uri = new Uri(uriString);
            return true;
        }
        catch (UriFormatException)
        {
            uri = null;
            return false;
        }
    }

    private void ClearCache()
    {
        foreach (CachedAudioClip cachedAudioClip in new List<CachedAudioClip>(audioClipCache.Values))
        {
            RemoveCachedAudioClip(cachedAudioClip);
        }
        audioClipCache.Clear();
    }

    private void AddAudioClipToCache(string path, AudioClip audioClip, bool streamAudio)
    {
        if (audioClipCache.Count >= CriticalCacheSize)
        {
            RemoveOldestAudioClipsFromCache();
        }

        // Cache the new AudioClip.
        CachedAudioClip cachedAudioClip = new(path, audioClip, Time.frameCount, streamAudio);
        audioClipCache[path] = cachedAudioClip;
    }

    private void RemoveOldestAudioClipsFromCache()
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

    private void RemoveCachedAudioClip(CachedAudioClip cachedAudioClip)
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

    private static UnityWebRequest CreateAudioClipRequest(Uri uriHandle, bool streamAudio)
    {
        UnityWebRequest webRequest = UnityWebRequestMultimedia.GetAudioClip(uriHandle, AudioType.UNKNOWN);
        DownloadHandlerAudioClip downloadHandler = webRequest.downloadHandler as DownloadHandlerAudioClip;
        downloadHandler.streamAudio = streamAudio;
        return webRequest;
    }
}
