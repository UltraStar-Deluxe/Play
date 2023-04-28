using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

// Handles loading and caching of AudioClips.
// Use this over AudioUtils because AudioUtils does not cache AudioClips.
public class AudioManager : AbstractSingletonBehaviour, INeedInjection
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        ClearCache();
    }

    private const string MusicAudioMixerName = "Music";
    private const string SfxAudioMixerName = "Sfx";
    private const string VolumeParameterName = "Volume";
    
    private static readonly int criticalCacheSize = 10;
    private static readonly Dictionary<string, CachedAudioClip> audioClipCache = new();

    public static AudioManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<AudioManager>();

    [InjectedInInspector]
    public AudioMixer mainAudioMixer;

    [InjectedInInspector]
    public AudioClip defaultButtonSound;
    
    [InjectedInInspector]
    public AudioClip songSelectSound;
    
    [InjectedInInspector]
    public AudioClip singingResultsRatingPopupSound;
    
    [Inject]
    private Settings settings;
    
    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        settings.ObserveEveryValueChanged(it => it.AudioSettings.SfxVolumePercent)
            .Subscribe(newValue => SetVolume(SfxAudioMixerName, newValue / 100f));
    }

    public static void PlaySoundEffect(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        AudioManager audioManager = Instance;
        if (audioManager == null
            || audioManager.settings.AudioSettings.SfxVolumePercent <= 0)
        {
            return;
        }
        
        GameObject sfxInstance = new GameObject($"Sfx '{clip.name}'");

        AudioSource source = sfxInstance.AddComponent<AudioSource>();
        source.clip = clip;
        source.Play();

        // set the mixer group (e.g. music, sfx, etc.)
        source.outputAudioMixerGroup = GetAudioMixerGroup(SfxAudioMixerName);

        // destroy after clip length
        Destroy(sfxInstance, clip.length);
    }
    
    public static AudioMixerGroup GetAudioMixerGroup(string groupName)
    {
        AudioManager audioManager = Instance;

        if (audioManager == null)
            return null;

        if (audioManager.mainAudioMixer == null)
            return null;

        AudioMixerGroup[] groups = audioManager.mainAudioMixer.FindMatchingGroups(groupName);

        foreach (AudioMixerGroup match in groups)
        {
            if (match.ToString() == groupName)
                return match;
        }
        return null;

    }
    // convert linear value between 0 and 1 to decibels
    public static float GetDecibelValue(float linearValue)
    {
        // commonly used for linear to decibel conversion
        float conversionFactor = 20f;

        float decibelValue = (linearValue != 0) ? conversionFactor * Mathf.Log10(linearValue) : -144f;
        return decibelValue;
    }

    // convert decibel value to a range between 0 and 1
    public static float GetLinearValue(float decibelValue)
    {
        float conversionFactor = 20f;

        return Mathf.Pow(10f, decibelValue / conversionFactor);

    }

    // converts linear value between 0 and 1 into decibels and sets AudioMixer level
    public static void SetVolume(string groupName, float linearValue)
    {
        AudioManager audioManager = Instance;
        if (audioManager == null)
            return;

        float decibelValue = GetDecibelValue(linearValue);
        if (audioManager.mainAudioMixer != null)
        {
            audioManager.mainAudioMixer.SetFloat(groupName + VolumeParameterName, decibelValue);
        }
    }

    // returns a value between 0 and 1 based on the AudioMixer's decibel value
    public static float GetVolume(string groupName)
    {
        AudioManager audioManager = Instance;
        if (audioManager == null)
            return 0f;

        float decibelValue = 0f;
        if (audioManager.mainAudioMixer != null)
        {
            audioManager.mainAudioMixer.GetFloat(groupName, out decibelValue);
        }
        return GetLinearValue(decibelValue);
    }
    
    public static void PlayButtonSound()
    {
        AudioManager audioManager = Instance;
        if (audioManager == null)
            return;

        PlaySoundEffect(audioManager.defaultButtonSound);
    }
    
    public static void PlaySongSelectSound()
    {
        AudioManager audioManager = Instance;
        if (audioManager == null)
            return;

        PlaySoundEffect(audioManager.songSelectSound);
    }

    public static void PlaySingingResultsRatingPopupSound()
    {
        AudioManager audioManager = Instance;
        if (audioManager == null)
            return;

        PlaySoundEffect(audioManager.singingResultsRatingPopupSound);
    }

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
        if (uri.IsNullOrEmpty())
        {
            return null;
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

        private readonly Stopwatch stopwatch;

        public LoadingAudioClip(string path, DownloadHandlerAudioClip downloadHandler, Action<AudioClip> callback)
        {
            this.Path = path;
            this.DownloadHandler = downloadHandler;
            this.Callbacks.Add(callback);

            stopwatch = new Stopwatch();
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
