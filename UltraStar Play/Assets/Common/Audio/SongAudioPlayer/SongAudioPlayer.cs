using System;
using System.Collections.Generic;
using System.IO;
using FfmpegUnity;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Video;

public class SongAudioPlayer : MonoBehaviour, INeedInjection
{
    // The playback position increase in milliseconds from one frame to the next to be counted as "jump".
    // An event is fired when jumping forward in the song.
    private const int MinForwardJumpOffsetInMillis = 500;

    [InjectedInInspector]
    public AudioSource audioSource;
    
    [InjectedInInspector]
    public VideoPlayer videoPlayer;

    [InjectedInInspector]
    public FfplayCommand ffplayCommandPrefab;

    private FfplayCommand ffplayCommand;
    private FfmpegPlayerVideoTexture ffmpegPlayerVideoTexture;

    private AudioManager audioManager;
    private WebViewManager webViewManager;
    private SceneNavigator sceneNavigator;
    private Settings settings;

    private readonly Lazy<MidiManager> midiManagerLazy = new(() => MidiManager.Instance);
    private MidiManager MidiManager => midiManagerLazy.Value;
    
    // The last frame in which the position in the song was calculated
    private int positionInSongInMillisFrame;

    private readonly Subject<double> playbackStoppedEventStream = new();
    public IObservable<double> PlaybackStoppedEventStream => playbackStoppedEventStream;

    private readonly Subject<double> playbackStartedEventStream = new();
    public IObservable<double> PlaybackStartedEventStream => playbackStartedEventStream;

    private readonly Subject<double> positionInSongEventStream = new();
    public IObservable<double> PositionInSongEventStream => positionInSongEventStream;
    
    private readonly Subject<float> playbackSpeedChangedEventStream = new();
    public IObservable<float> PlaybackSpeedChangedEventStream => playbackSpeedChangedEventStream;

    private readonly Subject<SongAudioLoadedEvent> loadedEventStream = new();
    public IObservable<SongAudioLoadedEvent> LoadedEventStream => loadedEventStream;

    private readonly List<string> videoPlayerErrorMessages = new();

    private Texture ffmpegRenderTexture;
    public Texture FfmpegRenderTexture
    {
        get => ffmpegRenderTexture;
        set
        {
            ffmpegRenderTexture = value;
            if (ffplayCommand != null
                && ffplayCommand.VideoTexture != null)
            {
                ffplayCommand.VideoTexture.VideoTexture = ffmpegRenderTexture;
            }
        }
    }

    public IObservable<Pair<double>> JumpBackInSongEventStream
    {
        get
        {
            return positionInSongEventStream.Pairwise().Where(pair => pair.Previous > pair.Current);
        }
    }

    public IObservable<Pair<double>> JumpForwardInSongEventStream
    {
        get
        {
            // The position will increase in normal playback. A big increase however, can always be considered as "jump".
            // Furthermore, when not currently playing, then every forward change can be considered as "jump".
            return positionInSongEventStream.Pairwise().Where(pair =>
            {
                return (pair.Previous + MinForwardJumpOffsetInMillis) < pair.Current
                    || (!IsPlaying && pair.Previous < pair.Current);
            });
        }
    }

    // The current position in the song in milliseconds.
    private double positionInSongInMillis;
    public double PositionInSongInMillis
    {
        get
        {
            if (audioSource == null
                || videoPlayer == null
                || webViewManager == null
                || !IsFullyLoaded)
            {
                return 0;
            }

            // The samples of an AudioClip change concurrently,
            // even when they are queried in the same frame (e.g. Update() of different scripts).
            // For a given frame, the position in the song should be the same for all scripts,
            // which is why the value is only updated once per frame.
            if (positionInSongInMillisFrame != Time.frameCount)
            {
                positionInSongInMillisFrame = Time.frameCount;
                positionInSongInMillis = PositionInSongInMillisExact;
            }
            return positionInSongInMillis;
        }

        set
        {
            if (DurationOfSongInMillis <= 0
                || double.IsNaN(value))
            {
                return;
            }

            double newPositionInSongInMillis = value;
            if (newPositionInSongInMillis < 0)
            {
                newPositionInSongInMillis = 0;
            }
            else if (newPositionInSongInMillis > DurationOfSongInMillis - 1)
            {
                newPositionInSongInMillis = DurationOfSongInMillis - 1;
            }

            positionInSongInMillis = newPositionInSongInMillis;
            lastSetPositionInSongInMillis = newPositionInSongInMillis;
            lastSetPositionInSongInMillisUnityTimeInSeconds = Time.time;

            float newPositionInSongInSeconds = (float)(newPositionInSongInMillis / 1000.0);
            
            if (AudioSupportProvider is EAudioSupportProvider.Ffmpeg
                && ffplayCommand != null)
            {
                // TODO: SeekTime is inaccurate, notably in the song editor.
                // Debug.Log($"SeekTime: {newPositionInSongInSeconds}");
                ffplayCommand.SeekTime(newPositionInSongInSeconds);
            }
            else if (AudioSupportProvider is EAudioSupportProvider.WebView)
            {
                webViewManager.SetPlaybackPositionInMillis(newPositionInSongInMillis);
            }
            else if (AudioSupportProvider is EAudioSupportProvider.UnityVideoPlayer)
            {
                 videoPlayer.time = newPositionInSongInSeconds;
            }
            else if (AudioSupportProvider is EAudioSupportProvider.UnityAudioSource)
            {
                audioSource.time = newPositionInSongInSeconds;
            }

            positionInSongEventStream.OnNext(positionInSongInMillis);
        }
    }
    
    // Workaround: SeekTime is not reliable. It jumps back to position 0 after few frames sometimes.
    private double lastSetPositionInSongInMillis;
    private float lastSetPositionInSongInMillisUnityTimeInSeconds;

    public double PositionInSongInSeconds
    {
        get => positionInSongInMillis / 1000.0;
        set => PositionInSongInMillis = value * 1000.0;
    }

    /**
     * Returns the exact position in the song based on current sample position.
     * Note that this changes concurrently,
     * such that it can return different values when called multiple times in the same frame.
     */
    public double PositionInSongInMillisExact
    {
        get
        {
            if (AudioSupportProvider is EAudioSupportProvider.Ffmpeg
                && ffplayCommand != null)
            {
                return ffplayCommand.CurrentTime * 1000.0;
            }
            else if (AudioSupportProvider is EAudioSupportProvider.WebView)
            {
                return webViewManager.EstimatedPlaybackPositionInMillis;
            }
            else if (AudioSupportProvider is EAudioSupportProvider.UnityVideoPlayer)
            {
                return videoPlayer.time * 1000.0;
            }
            else if (AudioSupportProvider is EAudioSupportProvider.UnityAudioSource)
            {
                return ((double)audioSource.timeSamples / (double)audioSource.clip.frequency) * 1000.0;
            }

            return 0;
        }
    }

    public double DurationOfSongInMillis { get; private set; }
    public double DurationOfSongInSeconds => DurationOfSongInMillis / 1000.0;
    public double DurationOfSongInBeats => BpmUtils.MillisecondInSongToBeat(SongMeta, DurationOfSongInMillis);

    /**
     * Position in the song from 0 (start of song) to 1 (end of song).
     */
    public double PositionInSongInPercent
    {
        get
        {
            if (DurationOfSongInMillis <= 0)
            {
                return 0;
            }

            return PositionInSongInMillis / DurationOfSongInMillis;
        }
    }

    public bool IsPlaying
    {
        get
        {
            return !isPaused
                   && ((AudioSupportProvider is EAudioSupportProvider.Ffmpeg && ffplayCommand != null && ffplayCommand.IsRunning && !ffplayCommand.Paused)
                       || (AudioSupportProvider is EAudioSupportProvider.WebView && webViewManager.IsPlaying)
                       || (AudioSupportProvider is EAudioSupportProvider.UnityVideoPlayer && videoPlayer.isPlaying)
                       || (AudioSupportProvider is EAudioSupportProvider.UnityAudioSource && audioSource.isPlaying));
        }
    }
    private bool isPaused;

    public bool IsPartiallyLoaded => AudioSupportProvider is not EAudioSupportProvider.None;
    public bool IsFullyLoaded => (AudioSupportProvider is EAudioSupportProvider.Ffmpeg && ffplayCommand != null && ffplayCommand.Duration > 0)
                                 || (AudioSupportProvider is EAudioSupportProvider.WebView && webViewManager.DurationInMillis > 0)
                                 || (AudioSupportProvider is EAudioSupportProvider.UnityVideoPlayer && videoPlayer.length > 0)
                                 || (AudioSupportProvider is EAudioSupportProvider.UnityAudioSource && audioSource.clip != null && audioSource.clip.length > 0); 
    
    private SongMeta SongMeta { get; set; }

    public float VolumeFactor
    {
        get
        {
            if (AudioSupportProvider is EAudioSupportProvider.Ffmpeg)
            {
                return ffplayCommand.AudioSourceComponent.volume;
            }
            else if (AudioSupportProvider is EAudioSupportProvider.WebView)
            {
                return webViewManager.VolumeInPercent / 100f;
            }
            else if (AudioSupportProvider is EAudioSupportProvider.UnityAudioSource or EAudioSupportProvider.UnityVideoPlayer)
            {
                return audioSource.volume;
            }

            return 1;
        }
        set
        {
            float oldValue = VolumeFactor;
            if (Math.Abs(oldValue - value) < 0.001f)
            {
                return;
            }
            
            if (AudioSupportProvider is EAudioSupportProvider.Ffmpeg
                && ffplayCommand != null)
            {
                ffplayCommand.AudioSourceComponent.volume = value;
            }
            else if (AudioSupportProvider is EAudioSupportProvider.WebView)
            {
                webViewManager.VolumeInPercent = (int)(value * 100);
            }
            else if (AudioSupportProvider is EAudioSupportProvider.UnityAudioSource or EAudioSupportProvider.UnityVideoPlayer)
            {
                audioSource.volume = value;
            }
        }
    }

    public float PlaybackSpeed
    {
        get
        {
            if (AudioSupportProvider is EAudioSupportProvider.UnityVideoPlayer)
            {
                return videoPlayer.playbackSpeed;
            }
            else if (AudioSupportProvider is EAudioSupportProvider.UnityAudioSource)
            {
                return audioSource.pitch;
            }

            return 1;
        }

        set
        {
            float oldPlaybackSpeed = PlaybackSpeed;

            // Limit playback speed. Allowed (and useful) is a range of 0.5 to 1.5.
            float newPlaybackSpeed = value;
            if (newPlaybackSpeed < 0.5f)
            {
                newPlaybackSpeed = 0.5f;
            }
            else if (newPlaybackSpeed > 1.5f)
            {
                newPlaybackSpeed = 1.5f;
            }

            // Set playback speed
            if (AudioSupportProvider is EAudioSupportProvider.UnityVideoPlayer)
            {
                videoPlayer.playbackSpeed = newPlaybackSpeed;
            }
            else if (AudioSupportProvider is EAudioSupportProvider.UnityAudioSource)
            {
                if (Math.Abs(newPlaybackSpeed - audioSource.pitch) < 0.01f)
                {
                    return;
                }
                
                AudioUtils.SetPitchWithPitchShifter(audioSource, newPlaybackSpeed);
            }

            // Fire change event
            if (Math.Abs(PlaybackSpeed - oldPlaybackSpeed) > 0.01f)
            {
                playbackSpeedChangedEventStream.OnNext(newPlaybackSpeed);
            }
        }
    }

    public EAudioSupportProvider AudioSupportProvider { get; private set; } = EAudioSupportProvider.None;

    private void Awake()
    {
        // Early fetch of dependencies.
        // This is needed because the SongAudioPlayer is called early in the scene setup.
        // TODO: This is a hack. Find a better way to do this (e.g. inject objects in order of their dependencies).
        audioManager = AudioManager.Instance;
        webViewManager = WebViewManager.Instance;
        sceneNavigator = SceneNavigator.Instance;
        settings = SettingsManager.Instance.Settings;
    }

    private void Start()
    {
        sceneNavigator.BeforeSceneChangeEventStream
            .Subscribe(_ =>
            {
                if (AudioSupportProvider is EAudioSupportProvider.WebView)
                {
                    PauseAudio();
                }
            })
            .AddTo(gameObject);

        // StartCoroutine(CoroutineUtils.ExecuteRepeatedlyInSeconds(0.5f, () =>
        //     Debug.Log($"Pos in song: {PositionInSongInMillis}")));
    }

    private void OnEnable()
    {
        videoPlayer.errorReceived += OnVideoPlayerErrorReceived;
    }

    private void OnDisable()
    {
        videoPlayer.errorReceived -= OnVideoPlayerErrorReceived;
    }

    private void Update()
    {
        // Set the video texture. Ffmpeg resets this sometimes (dont know why).
        if (AudioSupportProvider is EAudioSupportProvider.Ffmpeg
            && ffplayCommand != null
            && ffmpegRenderTexture != null
            && ffmpegPlayerVideoTexture.VideoTexture != ffmpegRenderTexture)
        {
            ffmpegPlayerVideoTexture.VideoTexture = ffmpegRenderTexture;
        }

        if (IsPlaying)
        {
            positionInSongEventStream.OnNext(PositionInSongInMillis);

            // Workaround: SeekTime is not reliable. It jumps back to position 0 after few frames sometimes.
            if (AudioSupportProvider is EAudioSupportProvider.Ffmpeg
                && ffplayCommand != null
                && !TimeUtils.IsDurationAboveThresholdInSeconds(lastSetPositionInSongInMillisUnityTimeInSeconds, 1f)
                && lastSetPositionInSongInMillis > 1000
                && PositionInSongInMillis < 1000)
            {
                Debug.Log($"Setting position in song again. ffmpeg seems to have ignored the last set value of {lastSetPositionInSongInMillis} ms and is not at {PositionInSongInMillis} ms.");
                ffplayCommand.SeekTime(lastSetPositionInSongInMillis / 1000.0);
            }
        }
    }

    public void LoadAndPlaySongAudio(
        SongMeta songMeta,
        double startPositionInMillis = 0,
        bool streamAudio = true)
    {
        LoadAndPlaySongAudioAsObservable(
                songMeta,
                startPositionInMillis,
                streamAudio)
            // Subscribe to trigger observable
            .Subscribe(evt => Debug.Log($"Successfully loaded song: {evt}"));
    }

    public IObservable<SongAudioLoadedEvent> LoadAndPlaySongAudioAsObservable(
        SongMeta songMeta,
        double startPositionInMillis = 0,
        bool streamAudio = true)
    {
        string audioUri = SongMetaUtils.GetAudioUri(songMeta);
        if (!SongMetaUtils.AudioResourceExists(songMeta))
        {
            return ObservableUtils.LogErrorThenThrow<SongAudioLoadedEvent>(
                new SongAudioPlayerException($"Audio resource does not exist: {audioUri}"));
        }

        SongMeta = songMeta;
        
        UnloadAudioAndVideo();
        
        string fileExtension = Path.GetExtension(audioUri);
        if (ApplicationUtils.IsUnitySupportedVideoFormat(fileExtension))
        {
            return LoadWithVideoPlayer(songMeta, audioUri, startPositionInMillis);
        }
        else if (ApplicationUtils.IsSupportedMidiFormat(fileExtension))
        {
            return LoadWithMidiManager(songMeta, audioUri, startPositionInMillis);
        }
        else if (ApplicationUtils.IsUnitySupportedAudioFormat(fileExtension))
        {
            return LoadWithAudioSource(songMeta, audioUri, startPositionInMillis, streamAudio);
        }
        else if (webViewManager.CanHandleUrl(audioUri))
        {
            return LoadWithWebView(songMeta, audioUri, startPositionInMillis);
        }
        else if (settings.UseFfmpegToPlayMediaFiles)
        {
            return LoadWithFfmpeg(songMeta, audioUri, startPositionInMillis);
        }
        else
        {
            return ObservableUtils.LogErrorThenThrow<SongAudioLoadedEvent>(
                new SongAudioPlayerException($"Unsupported audio resource '{audioUri}'."));
        }
    }

    private void UnloadAudioAndVideo()
    {
        AudioSupportProvider = EAudioSupportProvider.None;
        
        StopAllCoroutines();

        DestroyFfmpegPlayer();

        videoPlayerErrorMessages.Clear();
        videoPlayer.Stop();
        videoPlayer.url = "";

        audioSource.Stop();
        audioSource.clip = null;
        
        webViewManager.PausePlayback();

        DurationOfSongInMillis = 0;
    }
    
    private IObservable<SongAudioLoadedEvent> LoadWithMidiManager(SongMeta songMeta, string audioUri,
        double startPositionInMillis)
    {
        AudioClip audioClip = MidiManager.CreateAudioClip(audioUri);
        if (audioClip == null)
        {
            audioSource.Stop();
            return ObservableUtils.LogErrorThenThrow<SongAudioLoadedEvent>(
                new SongAudioPlayerException($"Failed to load audio clip from MIDI file {audioUri}"));
        }

        return Observable.Create<SongAudioLoadedEvent>(o =>
        {
            AudioSupportProvider = EAudioSupportProvider.UnityAudioSource;
            audioSource.clip = audioClip;
            DurationOfSongInMillis = 1000.0 * audioClip.samples / audioClip.frequency;
            PositionInSongInMillis = startPositionInMillis;
            audioSource.Play();
            FireLoadedEvent(o, songMeta, audioUri);
            return Disposable.Empty;
        });
    }
    
    private IObservable<SongAudioLoadedEvent> LoadWithAudioSource(
        SongMeta songMeta,
        string audioUri,
        double startPositionInMillis,
        bool streamAudio)
    {
        return Observable.Create<SongAudioLoadedEvent>(o =>
        {
            AudioSupportProvider = EAudioSupportProvider.UnityAudioSource;
            audioManager.LoadAudioClipFromUri(audioUri, streamAudio)
                .CatchIgnore((Exception error) => o.OnError(error))
                .Subscribe(loadedAudioClip =>
                {
                    if (loadedAudioClip == null)
                    {
                        audioSource.Stop();
                        string errorMessage = $"Failed to load audio clip from {audioUri}";
                        Debug.LogError(errorMessage);
                        o.OnError(new SongAudioPlayerException(errorMessage));
                        return;
                    }

                    audioSource.clip = loadedAudioClip;
                    DurationOfSongInMillis = 1000.0 * loadedAudioClip.samples / loadedAudioClip.frequency;
                    PositionInSongInMillis = startPositionInMillis;
                    FireLoadedEvent(o, songMeta, audioUri);
                })
                .AddTo(gameObject);
            
            return Disposable.Empty;
        });
    }

    private IObservable<SongAudioLoadedEvent> LoadWithVideoPlayer(SongMeta songMeta, string audioUri, double startPositionInMillis)
    {
        videoPlayer.url = audioUri;
        if (videoPlayer.url.IsNullOrEmpty())
        {
            videoPlayer.Stop();
            return ObservableUtils.LogErrorThenThrow<SongAudioLoadedEvent>(
                new SongAudioPlayerException($"Failed to load video from {audioUri}"));
        }

        AudioSupportProvider = EAudioSupportProvider.UnityVideoPlayer;
        
        // Must play the video to trigger loading.
        videoPlayer.Play();
        
        // The video is loaded asynchronously. The length property of the VideoPlayer indicates whether it has been loaded.
        return Observable.Create<SongAudioLoadedEvent>(o =>
        {
            StartCoroutine(CoroutineUtils.ExecuteWhenConditionIsTrue(
                () => videoPlayer.length > 0 || videoPlayerErrorMessages.Count > 0,
                () =>
                {
                    if (videoPlayerErrorMessages.Count > 0)
                    {
                        UnloadAudioAndVideo();
                        Debug.Log($"Failed to load audio with Unity's VideoPlayer. Trying to load it with ffmpeg. URI: {audioUri}");
                        LoadWithFfmpeg(songMeta, audioUri, startPositionInMillis)
                            .Subscribe(o.OnNext, o.OnError, o.OnCompleted);
                        return;
                    }
                    
                    DurationOfSongInMillis = 1000.0 * videoPlayer.length;
                    PositionInSongInMillis = startPositionInMillis;
                    
                    // Play the audio of the video player through the AudioSource.
                    videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                    for (ushort trackIndex = 0; trackIndex < videoPlayer.audioTrackCount; trackIndex++)
                    {
                        Debug.Log($"videoPlayer.SetTargetAudioSource: trackIndex: {trackIndex}, audioSource: {audioSource}");
                        videoPlayer.SetTargetAudioSource(trackIndex, audioSource);
                    }
                    audioSource.Play();
                    
                    FireLoadedEvent(o, songMeta, audioUri);
                }));
            return Disposable.Empty;
        });
    }

    private IObservable<SongAudioLoadedEvent> LoadWithFfmpeg(SongMeta songMeta, string audioUri, double startPositionInMillis)
    {
        Debug.Log($"SongAudioPlayer loading audio via ffmpeg: '{audioUri}', start pos: {startPositionInMillis} ms");

        try
        {
            // Destroy old ffmpeg player
            DestroyFfmpegPlayer();

            // Instantiate new ffmpeg player
            ffplayCommand = Instantiate(ffplayCommandPrefab, transform);
            ffmpegPlayerVideoTexture = ffplayCommand.GetComponentInChildren<FfmpegPlayerVideoTexture>();
            ffplayCommand.InputPath = audioUri;
            ffplayCommand.AudioSourceComponent.volume = VolumeFactor;
            if (FfmpegRenderTexture != null
                && ffplayCommand.VideoTexture != null)
            {
                ffplayCommand.VideoTexture.VideoTexture = FfmpegRenderTexture;
            }
            ffplayCommand.Play();

            AudioSupportProvider = EAudioSupportProvider.Ffmpeg;
        }
        catch (Exception e)
        {
            try
            {
                ffplayCommand?.Stop();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to stop ffmpeg player after failing to load '{audioUri}' using ffmpeg");
            }
            
            Debug.LogException(e);
            Debug.LogError($"Failed to load '{audioUri}' using ffmpeg");
            return ObservableUtils.LogErrorThenThrow<SongAudioLoadedEvent>(
                new SongAudioPlayerException($"Failed to load '{audioUri}'"));
        }
        
        // The video is loaded asynchronously.
        // The duration property indicates whether it has been loaded.
        float unityTimeInSecondsWhenStartedLoading = Time.time;
        return Observable.Create<SongAudioLoadedEvent>(o =>
        {
            StartCoroutine(CoroutineUtils.ExecuteWhenConditionIsTrue(
                () => ffplayCommand == null || ffplayCommand.Duration > 0,
                () =>
                {
                    if (ffplayCommand == null)
                    {
                        o.OnError(new SongAudioPlayerException("Failed to load file with ffmpeg. FfplayCommand is null"));
                        return;
                    }

                    double durationInSeconds = ffplayCommand.Duration;
                    DurationOfSongInMillis = durationInSeconds * 1000.0;

                    if (unityTimeInSecondsWhenStartedLoading > lastSetPositionInSongInMillisUnityTimeInSeconds)
                    {
                        // Jump to the start position if no new position was set in the meantime.
                        double startPositionInSeconds = startPositionInMillis / 1000.0;
                        ffplayCommand.SeekTime(startPositionInSeconds);
                    }
                    
                    FireLoadedEvent(o, songMeta, audioUri);
                }));
            return Disposable.Empty;
        });
    }

    private void DestroyFfmpegPlayer()
    {
        if (ffplayCommand == null)
        {
            return;
        }

        if (ffplayCommand.AudioSourceComponent != null)
        {
            ffplayCommand.AudioSourceComponent.mute = true;
        }
        ffplayCommand.gameObject.SetActive(false);
        if (Application.isEditor && !Application.isPlaying)
        {
            DestroyImmediate(ffplayCommand.gameObject);
        }
        else
        {
            Destroy(ffplayCommand.gameObject);
        }
        ffplayCommand = null;
    }

    private IObservable<SongAudioLoadedEvent> LoadWithWebView(SongMeta songMeta, string audioUri,
        double startPositionInMillis)
    {

        bool success = webViewManager.LoadUrl(audioUri);
        if (!success)
        {
            return ObservableUtils.LogErrorThenThrow<SongAudioLoadedEvent>(
                new SongAudioPlayerException($"Failed to load audio via WebView with URL {audioUri}"));
        }
        
        AudioSupportProvider = EAudioSupportProvider.WebView;
        
        // The WebView is loaded asynchronously. When the duration is available then the audio is loaded.
        long startTime = TimeUtils.GetUnixTimeMilliseconds();
        long timeoutInMillis = 5000;
        return Observable.Create<SongAudioLoadedEvent>(o =>
        {
            StartCoroutine(CoroutineUtils.ExecuteWhenConditionIsTrue(
                () =>
                {
                    return webViewManager.DurationInMillis > 0
                           || TimeUtils.IsDurationAboveThresholdInMillis(startTime, timeoutInMillis);
                },
                () =>
                {
                    if (TimeUtils.IsDurationAboveThresholdInMillis(startTime, timeoutInMillis))
                    {
                        Debug.Log("Loading audio using WebView timed out.");
                        return;
                    }
                    
                    DurationOfSongInMillis = webViewManager.DurationInMillis;
                    PositionInSongInMillis = startPositionInMillis;
                    PauseAudio();
                    FireLoadedEvent(o, songMeta, audioUri);
                }));

            return Disposable.Empty;
        });
    }

    private void FireLoadedEvent(IObserver<SongAudioLoadedEvent> o, SongMeta songMeta, string audioUri)
    {
        o.OnNext(new SongAudioLoadedEvent(songMeta, audioUri));
        loadedEventStream.OnNext(new SongAudioLoadedEvent(songMeta, audioUri));
    }
    
    public void ReloadAudio()
    {
        LoadAndPlaySongAudio(SongMeta);
    }

    public void StopAudio()
    {
        if (AudioSupportProvider is EAudioSupportProvider.Ffmpeg
            && ffplayCommand != null)
        {
            ffplayCommand.Stop();
        }
        else if (AudioSupportProvider is EAudioSupportProvider.WebView)
        {
            webViewManager.PausePlayback();
        }
        else if (AudioSupportProvider is EAudioSupportProvider.UnityVideoPlayer)
        {
            // The audio output is redirected to the AudioSource. Thus, stop VideoPlayer and AudioSource.
            videoPlayer.Stop();
            audioSource.Stop();
        }
        else if (AudioSupportProvider is EAudioSupportProvider.UnityAudioSource)
        {
            audioSource.Stop();
        }
    }
    
    public void PauseAudio()
    {
        if (!IsPlaying)
        {
            return;
        }

        if (AudioSupportProvider is EAudioSupportProvider.Ffmpeg
            && ffplayCommand != null)
        {
            if (ffplayCommand.IsRunning
                && !ffplayCommand.Paused)
            {
                ffplayCommand.TogglePause();
            }
        }
        else if (AudioSupportProvider is EAudioSupportProvider.UnityVideoPlayer)
        {
            // The audio output is redirected to the AudioSource. Thus, pause VideoPlayer and AudioSource.
            videoPlayer.Pause();
            audioSource.Pause();
        }
        else if (AudioSupportProvider is EAudioSupportProvider.WebView)
        {
            webViewManager.PausePlayback();
        }
        else if (AudioSupportProvider is EAudioSupportProvider.UnityAudioSource)
        {
            audioSource.Pause();
        }
        isPaused = true;
        playbackStoppedEventStream.OnNext(PositionInSongInMillis);
    }

    public void PlayAudio()
    {
        if (IsPlaying
            || !IsPartiallyLoaded)
        {
            return;
        }
        
        if (AudioSupportProvider is EAudioSupportProvider.Ffmpeg
            && ffplayCommand != null)
        {
            if (ffplayCommand.Paused)
            {
                ffplayCommand.TogglePause();
            }
        }
        else if (AudioSupportProvider is EAudioSupportProvider.WebView)
        {
            webViewManager.ResumePlayback();
        }
        else if (AudioSupportProvider is EAudioSupportProvider.UnityVideoPlayer)
        {
            // The audio output is redirected to the AudioSource. Thus, start VideoPlayer and AudioSource.
            videoPlayer.Play();
            audioSource.Play();
        }
        else if (AudioSupportProvider is EAudioSupportProvider.UnityAudioSource)
        {
            audioSource.Play();
        }
        isPaused = false;
        playbackStartedEventStream.OnNext(PositionInSongInMillis);
    }

    public double GetCurrentBeat(bool allowNegativeResult)
    {
        if (!IsFullyLoaded)
        {
            return 0;
        }

        double millisInSong = PositionInSongInMillis;
        double result = BpmUtils.MillisecondInSongToBeat(SongMeta, millisInSong);
        if (result < 0
            && !allowNegativeResult)
        {
            result = 0;
        }
        return result;
    }

    private void OnVideoPlayerErrorReceived(VideoPlayer source, string message)
    {
        Debug.LogError($"SongAudioPlayer received VideoPlayer error: {message}");
        videoPlayerErrorMessages.Add(message);   
    }
}
