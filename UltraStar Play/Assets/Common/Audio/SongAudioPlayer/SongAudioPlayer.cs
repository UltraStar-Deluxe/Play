using System;
using System.IO;
using UniRx;
using UnityEngine;
using UnityEngine.Video;

public class SongAudioPlayer : MonoBehaviour
{
    // The playback position increase in milliseconds from one frame to the next to be counted as "jump".
    // An event is fired when jumping forward in the song.
    private const int MinForwardJumpOffsetInMillis = 500;

    private readonly Lazy<AudioManager> audioManagerLazy = new(() => GameObjectUtils.FindComponentWithTag<AudioManager>("AudioManager"));
    private AudioManager AudioManager => audioManagerLazy.Value;

    private readonly LazyFromComponent<AudioSource> audioPlayerLazy = new(ctx => ctx.GetComponentInChildren<AudioSource>());
    private AudioSource AudioPlayer => audioPlayerLazy.GetValue(this);
    
    private readonly LazyFromComponent<VideoPlayer> videoPlayerLazy = new(ctx => ctx.GetComponentInChildren<VideoPlayer>());
    private VideoPlayer VideoPlayer => videoPlayerLazy.GetValue(this);

    private readonly LazyFromComponent<WebViewManager> webViewManagerLazy = new(ctx => WebViewManager.Instance);
    private WebViewManager WebViewManager => webViewManagerLazy.GetValue(this);
    
    private SceneNavigator SceneNavigator => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SceneNavigator>();
    
    private MidiManager MidiManager => MidiManager.Instance;
    
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
            if (AudioPlayer == null
                || VideoPlayer == null
                || WebViewManager == null
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
            if (DurationOfSongInMillis <= 0)
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

            float newTimeInSeconds = (float)(value / 1000.0);
            
            if (HasAudio)
            {
                AudioPlayer.time = newTimeInSeconds;
            }
            else if (HasVideo)
            {
                 VideoPlayer.time = newTimeInSeconds;
            }
            else if (HasWebView)
            {
                WebViewManager.SetPlaybackPositionInMillis(newPositionInSongInMillis);
            }

            positionInSongEventStream.OnNext(positionInSongInMillis);
        }
    }

    public double PositionInSongInSeconds => positionInSongInMillis / 1000.0;

    /**
     * Returns the exact position in the song based on current sample position.
     * Note that this changes concurrently,
     * such that it can return different values when called multiple times in the same frame.
     */
    public double PositionInSongInMillisExact
    {
        get
        {
            if (HasAudio)
            {
                return ((double)AudioPlayer.timeSamples / (double)AudioPlayer.clip.frequency) * 1000.0;
            }
            else if (HasVideo)
            {
                return VideoPlayer.time * 1000.0;
            }
            else if (HasWebView)
            {
                return WebViewManager.EstimatedPlaybackPositionInMillis;
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
                   && (
                       (HasAudio && AudioPlayer.isPlaying)
                       || (HasVideo && VideoPlayer.isPlaying)
                       || (HasWebView && WebViewManager.IsPlaying));
        }
    }

    public bool IsPartiallyLoaded => HasAudio || HasVideo || HasWebView;
    public bool IsFullyLoaded => (HasAudio && AudioPlayer.clip.length > 0) 
                                 || (HasVideo && VideoPlayer.length > 0)
                                 || (HasWebView && WebViewManager.DurationInMillis > 0);
    
    private SongMeta SongMeta { get; set; }

    public float VolumeFactor
    {
        get
        {
            if (HasWebView)
            {
                return WebViewManager.VolumeInPercent / 100f;
            }
            else
            {
                return AudioPlayer.volume;
            }
        }
        set
        {
            if (HasWebView)
            {
                WebViewManager.VolumeInPercent = (int)(value * 100);
            }
            else
            {
                AudioPlayer.volume = value;
            }
        }
    }

    public float Pitch
    {
        get
        {
            if (HasWebView)
            {
                return 1;
            }
            return AudioPlayer.pitch;
        }
        set
        {
            if (HasWebView)
            {
                return;
            }
            AudioPlayer.pitch = value;
        }
    }

    public float PlaybackSpeed
    {
        get
        {
            if (HasWebView)
            {
                return 1;
            }
            return AudioPlayer.pitch;
        }
        set
        {
            if (HasWebView)
            {
                return;
            }
            
            // Playback speed cannot be set randomly. Allowed (and useful) is a range of 0.5 to 1.5.
            float newPlaybackSpeed = value;
            if (newPlaybackSpeed < 0.5f)
            {
                newPlaybackSpeed = 0.5f;
            }
            else if (newPlaybackSpeed > 1.5f)
            {
                newPlaybackSpeed = 1.5f;
            }

            if (Math.Abs(newPlaybackSpeed - AudioPlayer.pitch) < 0.01f)
            {
                return;
            }
            
            AudioUtils.SetPitchWithPitchShifter(AudioPlayer, newPlaybackSpeed);

            playbackSpeedChangedEventStream.OnNext(newPlaybackSpeed);
        }
    }

    private bool HasAudio => AudioPlayer.clip != null;
    private bool HasVideo => !VideoPlayer.url.IsNullOrEmpty();
    private bool HasWebView => isWebViewAudio;

    private bool isPaused;

    private bool isWebViewAudio;

    private void Start()
    {
        SceneNavigator.BeforeSceneChangeEventStream
            .Subscribe(_ =>
            {
                if (isWebViewAudio)
                {
                    PauseAudio();
                }
            })
            .AddTo(gameObject);
    }
    
    private void Update()
    {
        if (IsPlaying)
        {
            positionInSongEventStream.OnNext(PositionInSongInMillis);
        }
    }

    public IObservable<SongAudioLoadedEvent> LoadSongAudio(SongMeta songMeta, double startPositionInMillis = 0, bool streamAudio = true)
    {
        string audioUri = SongMetaUtils.GetAudioUri(songMeta);
        if (!SongMetaUtils.AudioResourceExists(songMeta))
        {
            Debug.Log($"Audio resource does not exist: {audioUri}");
            return Observable.Throw<SongAudioLoadedEvent>(new Exception($"Audio resource does not exist: {audioUri}"));
        }

        SongMeta = songMeta;
        
        ResetAudioAndVideo();
        
        string fileExtension = Path.GetExtension(audioUri);
        if (ApplicationUtils.IsSupportedVideoFormat(fileExtension))
        {
            return LoadAsVideo(songMeta, audioUri, startPositionInMillis);
        }
        else if (ApplicationUtils.IsSupportedMidiFormat(fileExtension))
        {
            return LoadMidiAsAudio(songMeta, audioUri, startPositionInMillis);
        }
        else if (WebViewManager.CanHandleUrl(audioUri))
        {
            return LoadAsWebView(songMeta, audioUri, startPositionInMillis);
        }
        else
        {
            return LoadAsAudio(songMeta, audioUri, startPositionInMillis, streamAudio);
        }
    }

    private void ResetAudioAndVideo()
    {
        isWebViewAudio = false;
        
        VideoPlayer.Stop();
        VideoPlayer.url = "";

        AudioPlayer.Stop();
        AudioPlayer.clip = null;
        
        WebViewManager.PausePlayback();

        DurationOfSongInMillis = 0;
    }
    
    private IObservable<SongAudioLoadedEvent> LoadMidiAsAudio(SongMeta songMeta, string audioUri,
        double startPositionInMillis)
    {
        AudioClip audioClip = MidiManager.CreateAudioClip(audioUri);
        if (audioClip == null)
        {
            AudioPlayer.Stop();
            return ObservableUtils.LogErrorThenThrow<SongAudioLoadedEvent>(
                new Exception($"Failed to load audio clip from MIDI file {audioUri}"));
        }

        return Observable.Create<SongAudioLoadedEvent>(o =>
        {
            AudioPlayer.clip = audioClip;
            DurationOfSongInMillis = 1000.0 * audioClip.samples / audioClip.frequency;
            PositionInSongInMillis = startPositionInMillis;
            FireLoadedEvent(o, songMeta, audioUri);
            return Disposable.Empty;
        });
    }
    
    private IObservable<SongAudioLoadedEvent> LoadAsAudio(
        SongMeta songMeta,
        string audioUri,
        double startPositionInMillis,
        bool streamAudio)
    {
        return Observable.Create<SongAudioLoadedEvent>(o =>
        {
            AudioManager.LoadAudioClipFromUri(audioUri, streamAudio)
                .CatchIgnore((Exception error) => o.OnError(error))
                .Subscribe(loadedAudioClip =>
                {
                    if (loadedAudioClip == null)
                    {
                        AudioPlayer.Stop();
                        string errorMessage = $"Failed to load audio clip from {audioUri}";
                        Debug.LogError(errorMessage);
                        o.OnError(new Exception(errorMessage));
                        return;
                    }

                    AudioPlayer.clip = loadedAudioClip;
                    DurationOfSongInMillis = 1000.0 * loadedAudioClip.samples / loadedAudioClip.frequency;
                    PositionInSongInMillis = startPositionInMillis;
                    FireLoadedEvent(o, songMeta, audioUri);
                })
                .AddTo(gameObject);
            
            return Disposable.Empty;
        });
    }

    private IObservable<SongAudioLoadedEvent> LoadAsVideo(SongMeta songMeta, string audioUri,
        double startPositionInMillis)
    {
        VideoPlayer.url = audioUri;
        if (VideoPlayer.url.IsNullOrEmpty())
        {
            VideoPlayer.Stop();
            return ObservableUtils.LogErrorThenThrow<SongAudioLoadedEvent>(
                new Exception($"Failed to load video from {audioUri}"));
        }
        
        // Play the audio of the video player through the AudioSource.
        VideoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        for (int trackIndex = 0; trackIndex < VideoPlayer.audioTrackCount; trackIndex++)
        {
            VideoPlayer.SetTargetAudioSource(0, AudioPlayer);
        }

        // Must play the video to trigger loading.
        VideoPlayer.Play();

        // The video is loaded asynchronously. The length property of the VideoPlayer indicates whether it has been loaded.
        return Observable.Create<SongAudioLoadedEvent>(o =>
        {
            StartCoroutine(CoroutineUtils.ExecuteWhenConditionIsTrue(
                () => VideoPlayer.length > 0,
                () =>
                {
                    DurationOfSongInMillis = 1000.0 * VideoPlayer.length;
                    PositionInSongInMillis = startPositionInMillis;
                    PauseAudio();
                    FireLoadedEvent(o, songMeta, audioUri);
                }));
            return Disposable.Empty;
        });
    }

    private IObservable<SongAudioLoadedEvent> LoadAsWebView(SongMeta songMeta, string audioUri,
        double startPositionInMillis)
    {
        bool success = WebViewManager.LoadUrl(audioUri);
        if (!success)
        {
            return ObservableUtils.LogErrorThenThrow<SongAudioLoadedEvent>(
                new Exception($"Failed to load audio via WebView with URL {audioUri}"));
        }
        
        isWebViewAudio = true;

        // The WebView is loaded asynchronously. When the duration is available then the audio is loaded.
        long startTime = TimeUtils.GetUnixTimeMilliseconds();
        long timeoutInMillis = 5000;
        return Observable.Create<SongAudioLoadedEvent>(o =>
        {
            StartCoroutine(CoroutineUtils.ExecuteWhenConditionIsTrue(
                () =>
                {
                    return WebViewManager.DurationInMillis > 0
                           || TimeUtils.IsDurationAboveThresholdInMillis(startTime, timeoutInMillis);
                },
                () =>
                {
                    if (TimeUtils.IsDurationAboveThresholdInMillis(startTime, timeoutInMillis))
                    {
                        Debug.Log("Loading audio using WebView timed out.");
                        return;
                    }
                    
                    DurationOfSongInMillis = WebViewManager.DurationInMillis;
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
        LoadSongAudio(SongMeta);
    }

    public void StopAudio()
    {
        VideoPlayer.Stop();
        AudioPlayer.Stop();
        WebViewManager.PausePlayback();
    }
    
    public void PauseAudio()
    {
        if (!IsPlaying)
        {
            return;
        }

        AudioPlayer.Pause();
        VideoPlayer.Pause();
        WebViewManager.PausePlayback();
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

        if (HasAudio)
        {
            AudioPlayer.Play();
        }
        else if (HasVideo)
        {
            VideoPlayer.Play();
        }
        else if (HasWebView)
        {
            WebViewManager.ResumePlayback();
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
}
