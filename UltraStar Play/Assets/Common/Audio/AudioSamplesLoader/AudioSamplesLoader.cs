using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AudioSamplesLoader : AbstractSingletonBehaviour
{
    public static AudioSamplesLoader Instance => DontDestroyOnLoadManager.Instance.DoFindComponentOrThrow<AudioSamplesLoader>();

    private FfmpegAudioSamplesLoader ffmpegAudioSamplesLoader;

    private readonly Dictionary<string, AudioClip> ffmpegLoadedAudioClips = new(); 
    
    protected override object GetInstance()
    {
        return Instance;
    }

    public async Awaitable<AudioClip> LoadAsAudioClip(string uri)
    {
        if (ApplicationUtils.IsUnitySupportedAudioFormat(Path.GetExtension(uri)))
        {
            return await AudioManager.LoadAudioClipFromUriAsync(uri, false);
        }

        return LoadAsAudioClipViaFfmpeg(uri);
    }

    private AudioClip LoadAsAudioClipViaFfmpeg(string uri)
    {
        // Cache lookup
        if (ffmpegLoadedAudioClips.TryGetValue(uri, out AudioClip cachedAudioClip))
        {
            return cachedAudioClip;
        }
        
        // Load
        if (!PlatformUtils.IsWindows)
        {
            throw new InvalidOperationException($"Cannot load audio samples of this format on this platform. uri: '{uri}'");
        }

        InitializeFfmpegAudioSamplesLoader();

        FfmpegAudioSamplesLoader.FfmpegAudioSamplesData ffmpegAudioSamplesData = ffmpegAudioSamplesLoader.Load(uri);
        AudioClip audioClip = AudioClip.Create(
            Path.GetFileName(uri),
            ffmpegAudioSamplesData.Samples.Length,
            ffmpegAudioSamplesData.Channels,
            ffmpegAudioSamplesData.SampleRate,
            _3D: false,
            stream: false);
        
        // Add to cache
        ffmpegLoadedAudioClips[uri] = audioClip;
        
        return audioClip;
    }

    private void InitializeFfmpegAudioSamplesLoader()
    {
        if (ffmpegAudioSamplesLoader != null)
        {
            return;
        }

        ffmpegAudioSamplesLoader = new();
        ffmpegAudioSamplesLoader.ConfigureFfmpeg(ApplicationUtils.GetStreamingAssetsPath("FfmpegLibraries/Windows"));
    }

    private void OnDestroy()
    {
        foreach (AudioClip audioClip in ffmpegLoadedAudioClips.Values)
        {
            Destroy(audioClip);
        }
        ffmpegLoadedAudioClips.Clear();
    }
}
