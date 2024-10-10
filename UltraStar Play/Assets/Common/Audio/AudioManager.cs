using System;
using System.Collections.Generic;
using System.IO;
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
    public static AudioManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<AudioManager>();

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

    public static AudioClip LoadAudioClipFromUriImmediately(string uri, bool streamAudio = true)
    {
        AudioClip result = null;
        // Load with busy waiting
        Instance.LoadAudioClipFromUri(uri, streamAudio, true)
            .CatchIgnore((Exception ex) => result = null)
            .Subscribe(audioClip => result = audioClip);
        return result;
    }

    public static IObservable<AudioClip> LoadAudioClipFromUri(string uri, bool streamAudio = true)
    {
        return Instance.LoadAudioClipFromUri(uri, streamAudio, false);
    }

    private IObservable<AudioClip> LoadAudioClipFromUri(string uri, bool streamAudio, bool busyWaiting)
    {
        if (uri.IsNullOrEmpty())
        {
            return ObservableUtils.LogExceptionThenThrow<AudioClip>(new NullReferenceException("Cannot load AudioClip, URI is null or empty"));
        }

        if (!ApplicationUtils.IsUnitySupportedAudioFormat(Path.GetExtension(uri)))
        {
            return Observable.Throw<AudioClip>(new IllegalArgumentException($"Cannot load AudioClip because the format is not supported by Unity. URI: '{uri}', supported formats: {ApplicationUtils.unitySupportedAudioFiles.JoinWith(", ")}"));
        }

        if (!TryGetUri(uri, out Uri uriObject))
        {
            return Observable.Throw<AudioClip>(new IllegalArgumentException($"URI is invalid. Maybe the file does not exist. URI: '{uri}'"));
        }

        if (audioClipCache.TryGetValue(uri, out CachedAudioClip cachedAudioClip)
            && (cachedAudioClip.StreamedAudioClip != null || cachedAudioClip.FullAudioClip))
        {
            if (streamAudio && cachedAudioClip.StreamedAudioClip != null)
            {
                return Observable.Return<AudioClip>(cachedAudioClip.StreamedAudioClip);
            }
            else if (!streamAudio && cachedAudioClip.FullAudioClip != null)
            {
                return Observable.Return<AudioClip>(cachedAudioClip.FullAudioClip);
            }
        }

        return Observable.Create<AudioClip>(o =>
        {
            // Send web request
            UnityWebRequest webRequest = AudioUtils.CreateAudioClipRequest(uriObject, streamAudio);
            webRequest.SendWebRequest();

            // Check web request result in coroutine
            Instance.StartCoroutine(CoroutineUtils.WebRequestCoroutine(webRequest,
                downloadHandler =>
                {
                    if (downloadHandler is DownloadHandlerAudioClip downloadHandlerAudioClip
                        && downloadHandlerAudioClip.audioClip != null)
                    {
                        AudioClip audioClip = downloadHandlerAudioClip.audioClip;
                        AddAudioClipToCache(uri, audioClip, streamAudio);

                        o.OnNext(audioClip);
                        o.OnCompleted();
                    }
                    else
                    {
                        o.OnError(new LoadAudioException($"Failed to load AudioClip from URI: '{uri}'"));
                    }
                },
                ex =>
                {
                    Debug.LogException(ex);
                    Debug.LogError($"Failed to load AudioClip from URI: '{uri}': {ex.Message}");
                    o.OnError(ex);
                },
                busyWaiting));

            return Disposable.Empty;
        });
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
}
