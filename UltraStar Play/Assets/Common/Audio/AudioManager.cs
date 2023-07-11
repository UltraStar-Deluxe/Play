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

    private static Dictionary<AudioClip, int> audioClipToLastPlayedFrameCount = new();

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

    [Inject]
    private UnityWebRequestManager unityWebRequestManager;
    
    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        settings.ObserveEveryValueChanged(it => it.SfxVolumePercent)
            .Subscribe(newValue => SetVolume(SfxAudioMixerName, newValue / 100f))
            .AddTo(gameObject);
    }

    public static void PlaySoundEffect(AudioClip clip, float volume = 1)
    {
        if (clip == null)
        {
            return;
        }

        if (audioClipToLastPlayedFrameCount.TryGetValue(clip, out int lastPlayedFrameCount)
            && lastPlayedFrameCount == Time.frameCount)
        {
            return;
        }
        audioClipToLastPlayedFrameCount[clip] = Time.frameCount;

        AudioManager audioManager = Instance;
        if (audioManager == null
            || audioManager.settings.SfxVolumePercent <= 0
            || volume <= 0)
        {
            return;
        }
        
        GameObject sfxInstance = new GameObject($"Sfx '{clip.name}'");

        AudioSource source = sfxInstance.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
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

        PlaySoundEffect(audioManager.defaultButtonSound, 0.5f);
    }
    
    public static void PlaySongSelectSound()
    {
        AudioManager audioManager = Instance;
        if (audioManager == null)
            return;

        PlaySoundEffect(audioManager.songSelectSound, 0.3f);
    }

    public static void PlaySingingResultsRatingPopupSound()
    {
        AudioManager audioManager = Instance;
        if (audioManager == null)
            return;

        PlaySoundEffect(audioManager.singingResultsRatingPopupSound, 0.5f);
    }

    public AudioClip LoadAudioClipFromUriImmediately(string uri, bool streamAudio)
    {
        if (uri.IsNullOrEmpty())
        {
            Debug.LogError("Cannot load AudioClip, URI is null or empty");
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

        AudioClip loadedAudioClip = AudioUtils.LoadUncachedAudioClipImmediately(uri, streamAudio);
        if (loadedAudioClip == null)
        {
            Debug.LogError($"Failed to load AudioClip from URI '{uri}'");
            return null;
        }
        
        cachedAudioClip = new(uri, loadedAudioClip, Time.frameCount, streamAudio);
        audioClipCache[uri] = cachedAudioClip;
        return loadedAudioClip;
    }

    public IObservable<AudioClip> LoadAudioClipFromUri(string uri, bool streamAudio = true)
    {
        if (uri.IsNullOrEmpty())
        {
            return ObservableUtils.LogErrorThenThrow<AudioClip>(new NullReferenceException("Cannot load AudioClip, URI is null or empty"));
        }

        return Observable.Create<AudioClip>(o =>
        {
            if (audioClipCache.TryGetValue(uri, out CachedAudioClip cachedAudioClip)
                && (cachedAudioClip.StreamedAudioClip != null || cachedAudioClip.FullAudioClip))
            {
                if (streamAudio && cachedAudioClip.StreamedAudioClip != null)
                {
                    o.OnNext(cachedAudioClip.StreamedAudioClip);
                    return Disposable.Empty;
                }
                else if (!streamAudio && cachedAudioClip.FullAudioClip != null)
                {
                    o.OnNext(cachedAudioClip.FullAudioClip);
                    return Disposable.Empty;
                }
            }

            LoadAndCacheAudioClip(uri, streamAudio)
                .CatchIgnore((Exception error) => o.OnError(error))
                .Subscribe(loadedAudioClip => o.OnNext(loadedAudioClip));
            return Disposable.Empty;
        });
    }

    public static void ClearCache()
    {
        foreach (CachedAudioClip cachedAudioClip in new List<CachedAudioClip>(audioClipCache.Values))
        {
            RemoveCachedAudioClip(cachedAudioClip);
        }
        audioClipCache.Clear();
    }
    
    private IObservable<AudioClip> LoadAndCacheAudioClip(string uri, bool streamAudio)
    {
        if (!ApplicationUtils.IsUnitySupportedAudioFormat(Path.GetExtension(uri)))
        {
            return Observable.Throw<AudioClip>(new IllegalStateException(
                $"Cannot load AudioClip because the format is not supported by Unity. URI: '{uri}', supported formats: {ApplicationUtils.unitySupportedAudioFiles.ToCsv()}"));
        }
        
        return Observable.Create<AudioClip>(o =>
        {
            Uri uriHandle = new Uri(uri);
            UnityWebRequest webRequest = AudioUtils.CreateAudioClipRequest(uriHandle, streamAudio);
            webRequest.SendWebRequest();
            unityWebRequestManager.AddUnityWebRequest(webRequest, 
                downloadHandler => 
                {
                    if (downloadHandler is DownloadHandlerAudioClip downloadHandlerAudioClip)
                    { 
                        AudioClip audioClip = downloadHandlerAudioClip.audioClip;
                        AddAudioClipToCache(uri, audioClip, streamAudio);
                        o.OnNext(audioClip);
                    }
                }, 
                error => 
                {
                    Debug.LogException(error);
                    Debug.LogError($"Failed to load AudioClip from URI: '{uri}': {error.Message}");
                    o.OnError(error);
                });

            return Disposable.Empty;
        });
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
    
    private class AudioClipRequestData
    {
        public string uri;
        public Action<AudioClip> onSuccess;
        public Action onFailure;
    }
}
