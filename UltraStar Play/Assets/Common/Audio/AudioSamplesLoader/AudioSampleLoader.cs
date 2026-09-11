using System;
using System.Collections.Generic;
using System.IO;using UniInject;
using UnityEngine;

public class AudioSampleLoader : AbstractSingletonBehaviour, INeedInjection
{
    public static AudioSampleLoader Instance => DontDestroyOnLoadManager.Instance.DoFindComponentOrThrow<AudioSampleLoader>();

    private const int MaxCachedAudioClipCount = 50;
    
    private readonly Dictionary<string, AudioClipCacheEntry> ffmpegLoadedAudioClips = new();
    
    private FfmpegAudioSampleLoader ffmpegAudioSampleLoader;

    [Inject]
    private Settings settings;
    
    protected override object GetInstance()
    {
        return Instance;
    }

    public async Awaitable<AudioClip> LoadAsAudioClip(string uri)
    {
        if (ApplicationUtils.IsUnitySupportedAudioFormat(Path.GetExtension(uri))
            && settings.UnityMediaApiUsage is not EApiUsage.Disabled
            // Unity API does not respect Replay Gain.
            && settings.ReplayGainMode is EReplayGainMode.Off)
        {
            // To query all audio samples, the AudioClip must not be streamed. All data must have been fully loaded.
            return await AudioManager.LoadAudioClipFromUriAsync(uri, false);
        }

        return LoadAsAudioClipViaFfmpeg(uri);
    }

    private AudioClip LoadAsAudioClipViaFfmpeg(string uri)
    {
        // Cache lookup
        if (ffmpegLoadedAudioClips.TryGetValue(uri, out AudioClipCacheEntry cacheEntry))
        {
            cacheEntry.UpdateLastUsedAt();
            return cacheEntry.AudioClip;
        }
        
        // Load
        if (!PlatformUtils.IsWindows)
        {
            throw new InvalidOperationException($"Cannot load audio samples of this format on this platform. uri: '{uri}'");
        }

        InitializeFfmpegAudioSamplesLoader();

        FfmpegAudioSampleLoader.FfmpegAudioSamplesData ffmpegAudioSamplesData = ffmpegAudioSampleLoader.Load(uri, replayGainMode: GetReplayGainMode());
        AudioClip audioClip = AudioClip.Create(
            Path.GetFileName(uri),
            ffmpegAudioSamplesData.Samples.Length / ffmpegAudioSamplesData.Channels,
            ffmpegAudioSamplesData.Channels,
            ffmpegAudioSamplesData.SampleRate,
            _3D: false,
            stream: false);
        audioClip.SetData(ffmpegAudioSamplesData.Samples, 0);
        
        // Evict least recently used entry if cache is full
        if (ffmpegLoadedAudioClips.Count >= MaxCachedAudioClipCount)
        {
            string leastRecentlyUsedKey = null;
            DateTime oldestLastUsedAt = DateTime.MaxValue;
            foreach (KeyValuePair<string, AudioClipCacheEntry> kvp in ffmpegLoadedAudioClips)
            {
                if (kvp.Value.LastUsedAt < oldestLastUsedAt)
                {
                    oldestLastUsedAt = kvp.Value.LastUsedAt;
                    leastRecentlyUsedKey = kvp.Key;
                }
            }
            if (leastRecentlyUsedKey != null)
            {
                Destroy(ffmpegLoadedAudioClips[leastRecentlyUsedKey].AudioClip);
                ffmpegLoadedAudioClips.Remove(leastRecentlyUsedKey);
            }
        }

        // Add to cache
        ffmpegLoadedAudioClips[uri] = new AudioClipCacheEntry(audioClip);
        
        return audioClip;
    }

    private FfmpegAudioSampleLoader.ReplayGainMode GetReplayGainMode()
    {
        switch (settings.ReplayGainMode)
        {
            case EReplayGainMode.Track: return FfmpegAudioSampleLoader.ReplayGainMode.Track;
            case EReplayGainMode.Album: return FfmpegAudioSampleLoader.ReplayGainMode.Album;
            default: return FfmpegAudioSampleLoader.ReplayGainMode.Off;
        }
    }

    private void InitializeFfmpegAudioSamplesLoader()
    {
        if (ffmpegAudioSampleLoader != null)
        {
            return;
        }

        ffmpegAudioSampleLoader = new();
        ffmpegAudioSampleLoader.ConfigureFfmpeg(ApplicationUtils.GetStreamingAssetsPath("FfmpegLibraries/Windows"));
    }

    public void ClearCache()
    {
        foreach (AudioClipCacheEntry entry in ffmpegLoadedAudioClips.Values)
        {
            Destroy(entry.AudioClip);
        }

        ffmpegLoadedAudioClips.Clear();
    }
    
    private void OnDestroy()
    {
        ClearCache();
    }
}
