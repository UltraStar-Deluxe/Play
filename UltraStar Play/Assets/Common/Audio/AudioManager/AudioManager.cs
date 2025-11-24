using System;
using System.Collections.Generic;
using System.IO;
using UniInject;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;

/**
 * Handles loading and caching of AudioClips.
 */
public class AudioManager : AbstractSingletonBehaviour, INeedInjection
{
    public static AudioManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<AudioManager>();

    /**
     * When the cache has reached this critical size
     * then unused sprites are searched in the scene and removed from memory.
     */
    private const int CriticalCacheSize = 10;
    private readonly Dictionary<string, CachedAudioClip> audioClipCache = new();
    private readonly Dictionary<string, RunningRequest> runningRequests = new();

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

        // Multiple requests should load the AudioClip only once.
        // And when eventually loaded, then all callers should receive the same instance.
        if (runningRequests.TryGetValue(uri, out RunningRequest runningRequest))
        {
            Log.Verbose(() => $"Awaiting already running request for AudioClip: uri '{uri}'");
            return await WaitForRunningRequest(runningRequest);
        }
        runningRequest = new();
        runningRequests[uri] = runningRequest;
        try
        {
            AudioClip audioClip = await DoLoadUncachedAudioClipFromUriAsync(uri, uriObject, streamAudio);
            runningRequest.result = audioClip;
            return audioClip;
        }
        catch (Exception e)
        {
            runningRequest.exception = e;
            throw;
        }
        finally
        {
            runningRequests.Remove(uri);
        }
    }

    private async Awaitable<AudioClip> DoLoadUncachedAudioClipFromUriAsync(string uri, Uri uriObject, bool streamAudio)
    {
        try
        {
            using UnityWebRequest webRequest = CreateAudioClipRequest(uriObject, streamAudio);
            await WebRequestUtils.SendWebRequestAsync(webRequest);

            AudioClip audioClip = DownloadHandlerAudioClip.GetContent(webRequest);
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

    public void ClearCache()
    {
        Log.Verbose(() => $"Clearing cache. current count: {audioClipCache.Count}");
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
            Log.Debug(() => $"Removed oldest AudioClip from cache. count after: {audioClipCache.Count}, path: '{oldest.Path}'");
        }
    }

    private void RemoveCachedAudioClip(CachedAudioClip cachedAudioClip)
    {
        audioClipCache.Remove(cachedAudioClip.Path);

        if (cachedAudioClip.StreamedAudioClip != null)
        {
            cachedAudioClip.StreamedAudioClip.UnloadAudioData();
            Destroy(cachedAudioClip.StreamedAudioClip);
        }

        if (cachedAudioClip.FullAudioClip != null)
        {
            cachedAudioClip.FullAudioClip.UnloadAudioData();
            Destroy(cachedAudioClip.FullAudioClip);
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
    
    private async Awaitable<AudioClip> WaitForRunningRequest(RunningRequest runningRequest)
    {
        while (runningRequest.result == null
               && runningRequest.exception == null)
        {
            await Awaitable.EndOfFrameAsync();
        }
        return runningRequest.result ?? throw runningRequest.exception;
    }
    
    // Custom solution to resolve multiple awaits because AwaitableCompletionSource did not work as expected.
    private class RunningRequest
    {
        public string uri;
        public AudioClip result;
        public Exception exception;
    }
}
