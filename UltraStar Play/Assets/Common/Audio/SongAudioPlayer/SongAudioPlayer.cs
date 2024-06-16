using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;

public class SongAudioPlayer : MonoBehaviour, INeedInjection, ISongMediaPlayer<SongAudioLoadedEvent>
{
    // The playback position increase in milliseconds from one frame to the next to be counted as "jump".
    // An event is fired when jumping forward in the song.
    private const int MinForwardJumpOffsetInMillis = 500;

    [Inject]
    private Settings settings;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private AudioSourceAudioSupportProvider audioSourceAudioSupportProvider;

    [Inject(SearchMethod = SearchMethods.GetComponentsInChildren)]
    private AbstractAudioSupportProvider[] audioSupportProviders;

    private IAudioSupportProvider currentAudioSupportProvider;
    public IAudioSupportProvider CurrentAudioSupportProvider => currentAudioSupportProvider;

    // The last frame in which the position in the song was calculated
    private int positionInMillisFrame;

    private readonly Subject<double> playbackStoppedEventStream = new();
    public IObservable<double> PlaybackStoppedEventStream => playbackStoppedEventStream;

    private readonly Subject<double> playbackStartedEventStream = new();
    public IObservable<double> PlaybackStartedEventStream => playbackStartedEventStream;

    private readonly Subject<double> positionEventStream = new();
    public IObservable<double> PositionEventStream => positionEventStream;

    private readonly Subject<double> playbackSpeedChangedEventStream = new();
    public IObservable<double> PlaybackSpeedChangedEventStream => playbackSpeedChangedEventStream;

    private readonly Subject<SongAudioLoadedEvent> loadedEventStream = new();
    public IObservable<SongAudioLoadedEvent> LoadedEventStream => loadedEventStream;

    public IObservable<Pair<double>> JumpBackEventStream
        => positionEventStream.Pairwise().Where(pair => pair.Previous > pair.Current);

    public IObservable<Pair<double>> JumpForwardEventStream
        // The position will increase in normal playback. A big increase however, can always be considered as "jump".
        // Furthermore, when not currently playing, then every forward change can be considered as "jump".
        => positionEventStream.Pairwise().Where(pair => (pair.Previous + MinForwardJumpOffsetInMillis) < pair.Current
                                                        || (!IsPlaying && pair.Previous < pair.Current));

    // The current position in the song in milliseconds.
    private double positionInMillis;
    public double PositionInMillis
    {
        get
        {
            if (!IsFullyLoaded)
            {
                return 0;
            }

            // The samples of an AudioClip change concurrently,
            // even when they are queried in the same frame (e.g. Update() of different scripts).
            // For a given frame, the position in the song should be the same for all scripts,
            // which is why the value is only updated once per frame.
            if (positionInMillisFrame != Time.frameCount)
            {
                positionInMillisFrame = Time.frameCount;
                positionInMillis = PositionInMillisExact;
            }
            return positionInMillis;
        }

        set
        {
            if (!IsFullyLoaded
                || double.IsNaN(value))
            {
                return;
            }

            currentAudioSupportProvider.PositionInMillis = NumberUtils.Limit(value, 0, DurationInMillis - 1);
            positionInMillis = PositionInMillisExact;
            positionEventStream.OnNext(positionInMillis);
        }
    }

    public double PositionInSeconds
    {
        get => positionInMillis / 1000.0;
        set => PositionInMillis = value * 1000.0;
    }

    /**
     * Returns the exact position in the song based on current sample position.
     * Note that this changes concurrently,
     * such that it can return different values when called multiple times in the same frame.
     */
    public double PositionInMillisExact
    {
        get
        {
            if (!IsFullyLoaded)
            {
                return 0;
            }
            double rawResult = currentAudioSupportProvider.PositionInMillis;
            double result = IsPlaying
                ? rawResult - settings.SystemAudioBackendDelayInMillis
                : rawResult;
            return Math.Max(0, result);
        }
    }

    public double DurationInMillis { get; private set; }
    public double DurationInSeconds => DurationInMillis / 1000.0;
    public double DurationInBeats => SongMetaBpmUtils.MillisToBeats(loadedSongMeta, DurationInMillis);

    /**
     * Position in the song from 0 (start of song) to 1 (end of song).
     */
    public double PositionInPercent
    {
        get
        {
            if (DurationInMillis <= 0)
            {
                return 0;
            }

            return PositionInMillis / DurationInMillis;
        }
    }

    private bool isPlaying;
    public bool IsPlaying => isPlaying;
    public bool IsPlayingOfAudioProvider => IsFullyLoaded && currentAudioSupportProvider.IsPlaying;

    public bool IsPartiallyLoaded => currentAudioSupportProvider != null;
    public bool IsFullyLoaded => IsPartiallyLoaded && DurationInMillis > 0 && loadedSongMeta != null;

    private SongMeta loadedSongMeta;

    private float volumeFactor = 1;
    public float VolumeFactor
    {
        get
        {
            return volumeFactor;
        }

        set
        {
            float oldValue = VolumeFactor;
            if (Math.Abs(oldValue - value) < 0.001f)
            {
                return;
            }
            volumeFactor = value;

            if (!IsPartiallyLoaded)
            {
                return;
            }
            currentAudioSupportProvider.VolumeFactor = value;
        }
    }

    /**
     * The playback speed.
     * Attempts to change tempo without affecting pitch by making use of AudioMixer effects.
     */
    public double PlaybackSpeed
    {
        get
        {
            if (!IsPartiallyLoaded)
            {
                return 1;
            }

            return currentAudioSupportProvider.PlaybackSpeed;
        }

        set
        {
            SetPlaybackSpeed(value, true);
        }
    }

    public RenderTexture FfmpegRenderTexture {
        get
        {
            return currentAudioSupportProvider is FfmpegAudioSupportProvider ffmpegAudioSupportProvider
                ? ffmpegAudioSupportProvider.FfmpegRenderTexture
                : null;
        }
        set
        {
            if (currentAudioSupportProvider is FfmpegAudioSupportProvider ffmpegAudioSupportProvider)
            {
                ffmpegAudioSupportProvider.FfmpegRenderTexture = value;
            }
        }
    }

    private float lastApplyPlaybackStateToAudioProviderTimeInSeconds;

    private void OnDestroy()
    {
        UnloadAudio();
    }

    private void Update()
    {
        if (IsPlaying)
        {
            positionEventStream.OnNext(PositionInMillis);
        }

        // Apply playback state to (sadly buggy) AudioSupportProviders (ffmpeg and vlc).
        if (TimeUtils.IsDurationAboveThresholdInSeconds(lastApplyPlaybackStateToAudioProviderTimeInSeconds, 1))
        {
            lastApplyPlaybackStateToAudioProviderTimeInSeconds = Time.time;
            if (DurationInMillis > 0
                && PositionInMillis < DurationInMillis - 100)
            {
                ApplyPlaybackStateToAudioProvider();
            }
            else
            {
                // The audio players stop automatically at the end of the song. This needs to be monitored.
                isPlaying = IsPlayingOfAudioProvider;
            }
        }
    }

    public void LoadAndPlay(
        SongMeta songMeta,
        double startPositionInMillis = 0,
        bool streamAudio = true)
    {
        LoadAndPlayAsObservable(
                songMeta,
                startPositionInMillis,
                streamAudio)
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to load audio '{songMeta.GetArtistDashTitle()}': {ex.Message}");
                NotificationManager.CreateNotification(Translation.Get(R.Messages.common_errorWithReason,
                    "reason", ex.Message));
            })
            // Subscribe to trigger observable
            .Subscribe(evt => Debug.Log($"Loaded audio: {evt.MediaUri}'"));
    }

    public IObservable<SongAudioLoadedEvent> LoadAndPlayAsObservable(SongMeta songMeta)
        => LoadAndPlayAsObservable(songMeta, 0);

    public IObservable<SongAudioLoadedEvent> LoadAndPlayAsObservable(
        SongMeta songMeta,
        double startPositionInMillis,
        bool streamAudio = true)
    {
        UnloadAudio();

        string audioUri = SongMetaUtils.GetAudioUri(songMeta);
        if (!SongMetaUtils.AudioResourceExists(songMeta))
        {
            return ObservableUtils.LogExceptionThenThrow<SongAudioLoadedEvent>(
                new SongAudioPlayerException($"Audio resource does not exist: {audioUri}"));
        }

        return DoLoadAndPlayAsObservable(audioUri, audioSupportProviders, streamAudio, startPositionInMillis)
            .Select(evt =>
            {
                loadedSongMeta = songMeta;
                DurationInMillis = currentAudioSupportProvider.DurationInMillis;
                currentAudioSupportProvider.VolumeFactor = VolumeFactor;
                if (IsPlaying)
                {
                    currentAudioSupportProvider.Play();
                }

                loadedEventStream.OnNext(new SongAudioLoadedEvent(songMeta, evt.AudioUri));
                return new SongAudioLoadedEvent(songMeta, evt.AudioUri);
            });
    }

    private IObservable<AudioLoadedEvent> DoLoadAndPlayAsObservable(
        string audioUri,
        IAudioSupportProvider[] availableAudioSupportProviders,
        bool streamAudio,
        double startPositionInMillis)
    {
        IAudioSupportProvider audioSupportProvider = availableAudioSupportProviders
            .FirstOrDefault(it => it.IsSupported(audioUri));
        if (audioSupportProvider == null)
        {
            return ObservableUtils.LogExceptionThenThrow<AudioLoadedEvent>(
                new SongAudioPlayerException($"Unsupported audio resource '{audioUri}'."));
        }

        Debug.Log($"Loading audio '{audioUri}' via {audioSupportProvider}");

        return Observable.Create<AudioLoadedEvent>(o =>
        {
            audioSupportProvider.LoadAsObservable(audioUri, streamAudio, startPositionInMillis)
                .CatchIgnore((Exception ex) =>
                {
                    Debug.LogException(ex);
                    IAudioSupportProvider[] remainingAudioSupportProviders = availableAudioSupportProviders
                        .Except(new List<IAudioSupportProvider>() { audioSupportProvider })
                        .ToArray();
                    Debug.LogError($"Failed to load audio '{audioUri}' via {audioSupportProvider}. Using one of {remainingAudioSupportProviders.JoinWith(", ")} as fallback: {ex.Message}");

                    if (remainingAudioSupportProviders.IsNullOrEmpty())
                    {
                        o.OnError(new VideoSupportProviderException($"Failed to load audio and no remaining audio support providers: {audioUri}"));
                        return;
                    }
                    DoLoadAndPlayAsObservable(audioUri, remainingAudioSupportProviders, streamAudio, startPositionInMillis)
                        .Subscribe(o.OnNext, o.OnError, o.OnCompleted);
                })
                .Subscribe(evt =>
                {
                    currentAudioSupportProvider = audioSupportProvider;
                    o.OnNext(evt);
                })
                .AddTo(gameObject);
            return Disposable.Empty;
        });
    }

    public void UnloadAudio()
    {
        StopAllCoroutines();
        StopAudio();

        currentAudioSupportProvider?.Unload();
        currentAudioSupportProvider = null;
        DurationInMillis = 0;
        loadedSongMeta = null;
    }

    private void FireLoadedEvent(IObserver<SongAudioLoadedEvent> o, SongMeta songMeta, string audioUri)
    {
        o.OnNext(new SongAudioLoadedEvent(songMeta, audioUri));
        loadedEventStream.OnNext(new SongAudioLoadedEvent(songMeta, audioUri));
    }

    public void ReloadAudio()
    {
        LoadAndPlay(loadedSongMeta);
    }

    private void StopAudio()
    {
        if (!IsPartiallyLoaded)
        {
            return;
        }

        currentAudioSupportProvider.Stop();

        playbackStoppedEventStream.OnNext(PositionInMillis);
    }

    public void PauseAudio()
    {
        if (!IsPartiallyLoaded
            || !IsPlaying)
        {
            return;
        }

        isPlaying = false;
        currentAudioSupportProvider.Pause();

        playbackStoppedEventStream.OnNext(PositionInMillis);
    }

    public void PlayAudio()
    {
        if (!IsPartiallyLoaded
            || IsPlaying)
        {
            return;
        }

        isPlaying = true;
        currentAudioSupportProvider.Play();

        playbackStartedEventStream.OnNext(PositionInMillis);
    }

    private void ApplyPlaybackStateToAudioProvider()
    {
        if (!IsFullyLoaded)
        {
            return;
        }

        if (IsPlaying
            && !currentAudioSupportProvider.IsPlaying)
        {
            Debug.Log($"{nameof(SongAudioPlayer)} should be playing, but {currentAudioSupportProvider} is not. Starting its playback now.");
            currentAudioSupportProvider.Play();
        }
        else if (!IsPlaying
                 && currentAudioSupportProvider.IsPlaying)
        {
            Debug.Log($"{nameof(SongAudioPlayer)} should not be playing, but {currentAudioSupportProvider} is. Pausing its playback now.");
            currentAudioSupportProvider.Pause();
        }
    }

    public double GetCurrentBeat(bool allowNegativeResult)
    {
        if (!IsFullyLoaded)
        {
            return 0;
        }

        double millisInSong = PositionInMillis;
        double result = SongMetaBpmUtils.MillisToBeats(loadedSongMeta, millisInSong);
        if (result < 0
            && !allowNegativeResult)
        {
            result = 0;
        }
        return result;
    }

    public void SetPlaybackSpeed(double newValue, bool changeTempoButKeepPitch)
    {
        if (!IsPartiallyLoaded)
        {
            return;
        }

        double oldPlaybackSpeed = PlaybackSpeed;

        // Limit playback speed. Allowed is a range of 0.5 (half speed) to 2 (double speed).
        double newPlaybackSpeed = NumberUtils.Limit(newValue, 0.5f, 2f);
        if (Math.Abs(newValue - oldPlaybackSpeed) < 0.01f)
        {
            return;
        }

        currentAudioSupportProvider.SetPlaybackSpeed(newValue, changeTempoButKeepPitch);

        // Fire event
        if (Math.Abs(PlaybackSpeed - oldPlaybackSpeed) > 0.01f)
        {
            playbackSpeedChangedEventStream.OnNext(newPlaybackSpeed);
        }
    }
}
