using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;

public class SongVideoPlayer : MonoBehaviour, INeedInjection, IInjectionFinishedListener, ISongMediaPlayer<SongVideoLoadedEvent>
{
    private static readonly HashSet<string> ignoredVideoFiles = new();

    private const int ImmediatePlaybackPositionSyncThresholdInMillis = 400;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        ignoredVideoFiles.Clear();
    }

    /**
     * SongAudioPlayer to synchronize the video with.
     */
    [InjectedInInspector]
    public SongAudioPlayer songAudioPlayer;

    [InjectedInInspector]
    public VideoPlayer videoPlayer;

    [Inject(SearchMethod = SearchMethods.GetComponentsInChildren)]
    private AbstractVideoSupportProvider[] videoSupportProviders;

    private IVideoSupportProvider currentVideoSupportProvider;
    public IVideoSupportProvider CurrentVideoSupportProvider => currentVideoSupportProvider;

    [Inject]
    private WebViewManager webViewManager;

    [Inject(UxmlName = R.UxmlNames.songVideoImage, Optional = true)]
    private VisualElement videoImageVisualElement;
    public VisualElement VideoImageVisualElement
    {
        get
        {
            return videoImageVisualElement;
        }
        set
        {
            videoImageVisualElement = value;
            if (IsPartiallyLoaded
                && videoImageVisualElement != null)
            {
                videoImageVisualElement.ShowByDisplay();
                videoImageVisualElement.style.opacity = 1;
            }
        }
    }

    [Inject(UxmlName = R.UxmlNames.songImage, Optional = true)]
    private VisualElement backgroundImageVisualElement;

    [Inject]
    private Settings settings;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private VlcManager vlcManager;

    public bool ForceSyncOnForwardJump { get; set; }

    private SongMeta loadedSongMeta;

    public bool IsPartiallyLoaded => currentVideoSupportProvider != null;
    public bool IsFullyLoaded => IsPartiallyLoaded && DurationInMillis > 0 && loadedSongMeta != null;

    public double DurationInMillis { get; private set; }

    public bool HasLoadedBackgroundImage { get; private set; }

    private float playbackSpeed = 1;
    public float PlaybackSpeed
    {
        get => playbackSpeed;
        set
        {
            if (!IsPartiallyLoaded
                || float.IsNaN(value))
            {
                return;
            }

            currentVideoSupportProvider.PlaybackSpeed = value;
        }
    }

    public double PositionInSeconds
    {
        get => PositionInMillis / 1000.0;
        set => PositionInMillis = value * 1000.0;
    }

    public double PositionInMillis
    {
        get
        {
            if (!IsPartiallyLoaded)
            {
                return 0;
            }

            return currentVideoSupportProvider.PositionInMillis;
        }

        set
        {
            if (!IsPartiallyLoaded
                || double.IsNaN(value))
            {
                return;
            }

            currentVideoSupportProvider.PositionInMillis = value;
        }
    }

    private float nextSyncTimeInSeconds;

    private bool freezeVideo;
    public bool FreezeVideo
    {
        get => freezeVideo;
        set
        {
            freezeVideo = value;
            SyncVideoWithMusic(false);
        }
    }

    private bool isLooping;
    public bool IsLooping
    {
        get => isLooping;
        set
        {
            if (!IsPartiallyLoaded)
            {
                return;
            }

            isLooping = value;
            currentVideoSupportProvider.IsLooping = value;
        }
    }

    private bool isPlaying;
    public bool IsPlaying => isPlaying;

    private bool IsPlayingOfVideoProvider => IsPartiallyLoaded && currentVideoSupportProvider.IsPlaying;

    private float lastApplyPlaybackStateToVideoProviderTimeInSeconds;

    public void OnInjectionFinished()
    {
        settings.ObserveEveryValueChanged(it => it.SongBackgroundScaleMode)
            .Subscribe(_ => UpdateBackgroundScaleMode())
            .AddTo(gameObject);

        sceneNavigator.BeforeSceneChangeEventStream
            .Subscribe(_ => UnloadVideo())
            .AddTo(gameObject);

        // Synchronize playback position with SongAudioPlayer
        songAudioPlayer.JumpBackEventStream.Subscribe(evt =>
        {
            if (Math.Abs(evt.Previous - evt.Current) > ImmediatePlaybackPositionSyncThresholdInMillis)
            {
                SyncVideoWithMusic(true);
            }
        })
        .AddTo(gameObject);

        songAudioPlayer.JumpForwardEventStream
        .Subscribe(evt =>
        {
            if (Math.Abs(evt.Previous - evt.Current) > ImmediatePlaybackPositionSyncThresholdInMillis)
            {
                SyncVideoWithMusic(true);
            }
        })
        .AddTo(gameObject);
    }

    private void Update()
    {
        if (!IsFullyLoaded)
        {
            return;
        }

        if (songAudioPlayer != null)
        {
            SyncVideoWithMusic(false);
        }
        else
        {
            Debug.Log("no audio player");
        }

        if (TimeUtils.IsDurationAboveThresholdInSeconds(lastApplyPlaybackStateToVideoProviderTimeInSeconds, 1))
        {
            lastApplyPlaybackStateToVideoProviderTimeInSeconds = Time.time;
            if (DurationInMillis > 0
                && PositionInMillis < DurationInMillis - 100)
            {
                ApplyPlaybackStateToVideoProvider();
            }
            else
            {
                // The video players stop automatically at the end of the song. This needs to be monitored.
                isPlaying = IsPlayingOfVideoProvider;
            }
        }
    }

    private IObservable<VideoLoadedEvent> DoLoadAndPlayVideoAsObservable(
        string videoUri,
        IVideoSupportProvider[] availableVideoSupportProviders,
        bool videoEqualsAudio)
    {
        UnloadVideo();

        IVideoSupportProvider videoSupportProvider = availableVideoSupportProviders
            .FirstOrDefault(it => it.IsSupported(videoUri, videoEqualsAudio));
        if (videoSupportProvider == null)
        {
            return ObservableUtils.LogExceptionThenThrow<VideoLoadedEvent>(
                    new SongVideoPlayerException($"Unsupported video resource '{videoUri}'."));
        }

        Debug.Log($"Loading video '{videoUri}' via {videoSupportProvider}");

        return Observable.Create<VideoLoadedEvent>(o =>
        {
            videoSupportProvider.LoadAsObservable(videoUri)
                .CatchIgnore((Exception ex) =>
                {
                    Debug.LogException(ex);
                    IVideoSupportProvider[] remainingVideoSupportProviders = availableVideoSupportProviders
                        .Except(new List<IVideoSupportProvider>() { videoSupportProvider })
                        .ToArray();
                    Debug.LogError($"Failed to load video '{videoUri}' via {videoSupportProvider}. Using one of {remainingVideoSupportProviders.JoinWith(", ")} as fallback: {ex.Message}");

                    if (remainingVideoSupportProviders.IsNullOrEmpty())
                    {
                        o.OnError(new VideoSupportProviderException($"Failed to load video and no remaining video support providers: {videoUri}"));
                        return;
                    }
                    DoLoadAndPlayVideoAsObservable(videoUri, remainingVideoSupportProviders, videoEqualsAudio)
                        .Subscribe(o.OnNext, o.OnError, o.OnCompleted);
                })
                .Subscribe(evt =>
                {
                    currentVideoSupportProvider = videoSupportProvider;
                    o.OnNext(evt);
                });
            return Disposable.Empty;
        });
    }

    private void ShowVideoImageVisualElement()
    {
        if (videoImageVisualElement != null)
        {
            videoImageVisualElement.ShowByDisplay();
            videoImageVisualElement.style.opacity = 1;
        }
    }

    public void UnloadVideo()
    {
        StopAllCoroutines();
        StopVideo();

        currentVideoSupportProvider?.Unload();
        currentVideoSupportProvider = null;
        DurationInMillis = 0;
        loadedSongMeta = null;
    }

    private void SyncVideoWithMusic(bool forceImmediateSync)
    {
        SyncVideoPlayPauseWithAudio();
        if (IsPlaying || forceImmediateSync)
        {
            SyncVideoPositionWithAudio(forceImmediateSync);
        }
    }

    private void SyncVideoPlayPauseWithAudio()
    {
        if (!IsFullyLoaded
            || !gameObject.activeInHierarchy)
        {
            return;
        }

        bool songAudioPlayerIsPlaying = songAudioPlayer == null || songAudioPlayer.IsPlaying;

        if ((!songAudioPlayerIsPlaying
             && IsPlaying)
            || (IsFullyLoaded
                && DurationInMillis <= songAudioPlayer.PositionInSeconds
                && !IsLooping)
            || FreezeVideo)
        {
            PauseVideo();
        }
        else if (songAudioPlayerIsPlaying
                 && !IsPlaying)

        {
            if (!IsWaitingForVideoGap(songAudioPlayer.PositionInMillis, loadedSongMeta.VideoGapInMillis))
            {
                PlayVideo();
                SyncVideoPositionWithAudio(true);
            }
        }
    }

    public void SyncVideoPositionWithAudio(bool forceImmediateSync)
    {
        if (!IsFullyLoaded
            || (!forceImmediateSync && nextSyncTimeInSeconds > Time.time))
        {
            return;
        }

        double positionInAudioInMillis = songAudioPlayer.PositionInMillis;
        double durationOfAudioInMillis = songAudioPlayer.DurationInMillis;
        if (IsWaitingForVideoGap(positionInAudioInMillis, loadedSongMeta.VideoGapInMillis))
        {
            return;
        }

        // Loop short videos
        IsLooping = DurationInMillis < durationOfAudioInMillis / 2;

        // Both, the smooth sync and immediate sync need some time.
        nextSyncTimeInSeconds = Time.time + 1;

        double targetPositionInMillis = (loadedSongMeta.VideoGapInMillis) + positionInAudioInMillis;
        if (IsLooping)
        {
            targetPositionInMillis %= DurationInMillis;
        }
        double timeDifferenceInMillis = targetPositionInMillis - PositionInMillis;

        if (FreezeVideo)
        {
            PlaybackSpeed = 0;
        }
        else
        {
            // A big mismatch is corrected immediately.
            // A short mismatch in video and song position is smoothed out by adjusting the playback speed of the video.
            if (forceImmediateSync || Math.Abs(timeDifferenceInMillis) > 3000)
            {
                // Correct the mismatch immediately.
                PositionInMillis = targetPositionInMillis;
                PlaybackSpeed = 1f;
            }
            else
            {
                // Smooth out the time difference over a duration of 2 seconds
                float newPlaybackSpeed = 1 + (float)(timeDifferenceInMillis / 2000);
                PlaybackSpeed = newPlaybackSpeed;
            }
        }
    }

    // Returns true if still waiting for the start of the video at the given position in the song.
    private bool IsWaitingForVideoGap(double positionInMillis, double videoGapInMillis)
    {
        // A negative video gap means this duration has to be waited before playing the video.
        return videoGapInMillis < 0 && positionInMillis < -videoGapInMillis;
    }

    public void ShowBackgroundImage(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return;
        }

        UnloadVideo();

        if (videoImageVisualElement != null)
        {
            videoImageVisualElement.HideByDisplay();
            videoImageVisualElement.style.opacity = 0;
        }

        SongMetaImageUtils.GetBackgroundOrCoverImageUri(songMeta)
            .Subscribe(uri => SetBackgroundImageFromUri(uri));
    }

    private void SetBackgroundImageFromUri(string uri)
    {
        if (uri.IsNullOrEmpty())
        {
            return;
        }

        ImageManager.LoadSpriteFromUri(uri)
            .Subscribe(loadedSprite =>
            {
                if (backgroundImageVisualElement != null)
                {
                    backgroundImageVisualElement.ShowByDisplay();
                    backgroundImageVisualElement.style.backgroundImage = new StyleBackground(loadedSprite);
                }
                HasLoadedBackgroundImage = true;
            });
    }

    public void ReloadVideo()
    {
        // This method is used in the SongEditor. But only on Standalone platform when the video file changed.
        LoadAndPlayAsObservable(loadedSongMeta)
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to reload video: {ex.Message}");
            })
            // Subscribe to trigger the observable
            .Subscribe(evt => Debug.Log($"Loaded video: {evt.MediaUri}"));
    }

    public void LoadAndPlayVideoOrShowBackgroundImage(SongMeta songMeta)
    {
        if (!HasVideoUri(songMeta)
            || IsSongVideoPlaybackDisabled())
        {
            ShowBackgroundImage(songMeta);
            return;
        }

        LoadAndPlayAsObservable(songMeta)
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to load video of '{songMeta.GetArtistDashTitle()}': {ex.Message}");
                NotificationManager.CreateNotification(Translation.Get(R.Messages.common_errorWithReason,
                    "reason",
                    ex.Message));
                ShowBackgroundImage(songMeta);
            })
            // Subscribe to trigger observable
            .Subscribe(evt =>
            {
                Debug.Log($"Successfully loaded video of song '{songMeta.GetArtistDashTitle()}'");

                if (loadedSongMeta.VideoGapInMillis > 0)
                {
                    // Positive VideoGap, thus skip the start of the video
                    PositionInMillis = loadedSongMeta.VideoGapInMillis;
                }

                ShowVideoImageVisualElement();
            });
    }

    private bool IsSongVideoPlaybackDisabled()
    {
        switch (settings.SongVideoPlayback)
        {
            case ESongVideoPlayback.DisabledInSongSelect:
                return sceneNavigator.CurrentScene is EScene.SongSelectScene;
            case ESongVideoPlayback.DisabledInSongSelectAndSing:
                return sceneNavigator.CurrentScene
                    is EScene.SongSelectScene
                    or EScene.SingScene;
            default:
                return false;
        }
    }

    public IObservable<SongVideoLoadedEvent> LoadAndPlayAsObservable(SongMeta songMeta)
    {
        UnloadVideo();

        // Use the audio URL as video if the WebView can handle it (e.g. a YouTube video).
        string videoUri = SongMetaUtils.GetVideoUriPreferAudioUriIfWebView(songMeta, WebViewUtils.CanHandleWebViewUrl);

        if (videoUri.IsNullOrEmpty())
        {
            return ObservableUtils.LogExceptionThenThrow<SongVideoLoadedEvent>(
                new SongVideoPlayerException($"Ignoring empty video resource"));
        }

        if (ignoredVideoFiles.Contains(songMeta.Video))
        {
            return ObservableUtils.LogExceptionThenThrow<SongVideoLoadedEvent>(
                new SongVideoPlayerException($"Ignoring video resource: '{videoUri}'"));
        }

        if (!SongMetaUtils.ResourceExists(songMeta, videoUri))
        {
            return ObservableUtils.LogExceptionThenThrow<SongVideoLoadedEvent>(
                new SongVideoPlayerException($"Video resource does not exist: {videoUri}"));
        }

        return DoLoadAndPlayVideoAsObservable(
                videoUri,
                videoSupportProviders,
                songMeta.Video == songMeta.Audio)
            .Select(evt =>
            {
                loadedSongMeta = songMeta;
                DurationInMillis = currentVideoSupportProvider.DurationInMillis;
                currentVideoSupportProvider.PositionInMillis = songAudioPlayer.PositionInMillis;
                currentVideoSupportProvider.SetTargetTexture(videoPlayer.targetTexture);
                PlayVideo();

                return new SongVideoLoadedEvent(songMeta, evt.VideoUri);
            });
    }

    private void UpdateBackgroundScaleMode()
    {
        if (!IsPartiallyLoaded)
        {
            return;
        }

        currentVideoSupportProvider.SetBackgroundScaleMode(settings.SongBackgroundScaleMode);
        switch (settings.SongBackgroundScaleMode)
        {
            case ESongBackgroundScaleMode.FitInside:
                if (videoImageVisualElement != null)
                {
                    videoImageVisualElement.style.unityBackgroundScaleMode = new StyleEnum<ScaleMode>(ScaleMode.ScaleToFit);
                }

                if (backgroundImageVisualElement != null)
                {
                    backgroundImageVisualElement.style.unityBackgroundScaleMode = new StyleEnum<ScaleMode>(ScaleMode.ScaleToFit);
                }

                break;
            case ESongBackgroundScaleMode.FitOutside:
                if (videoImageVisualElement != null)
                {
                    videoImageVisualElement.style.unityBackgroundScaleMode = new StyleEnum<ScaleMode>(ScaleMode.ScaleAndCrop);
                }

                if (backgroundImageVisualElement != null)
                {
                    backgroundImageVisualElement.style.unityBackgroundScaleMode = new StyleEnum<ScaleMode>(ScaleMode.ScaleAndCrop);
                }

                break;
        }
    }

    private void ApplyPlaybackStateToVideoProvider()
    {
        if (!IsFullyLoaded)
        {
            return;
        }

        if (IsPlaying
            && !currentVideoSupportProvider.IsPlaying)
        {
            Debug.Log($"{nameof(SongVideoPlayer)} should be playing, but {currentVideoSupportProvider} is not. Starting its playback now.");
            currentVideoSupportProvider.Play();
        }
        else if (!IsPlaying
                 && currentVideoSupportProvider.IsPlaying)
        {
            Debug.Log($"{nameof(SongVideoPlayer)} should not be playing, but {currentVideoSupportProvider} is. Pausing its playback now.");
            currentVideoSupportProvider.Pause();
        }
    }

    private void PlayVideo()
    {
        SetPlaying(true);
    }

    private void PauseVideo()
    {
        SetPlaying(false);
    }

    private void StopVideo()
    {
        if (!IsPartiallyLoaded)
        {
            return;
        }

        currentVideoSupportProvider.Stop();
    }

    private void SetPlaying(bool value)
    {
        if (!IsPartiallyLoaded)
        {
            return;
        }

        isPlaying = value;
        currentVideoSupportProvider.IsPlaying = value;
    }

    public static void AddIgnoredVideoFile(string uri)
    {
        ignoredVideoFiles.Add(uri);
    }

    private static bool HasVideoUri(SongMeta songMeta)
    {
        string videoUri = SongMetaUtils.GetVideoUriPreferAudioUriIfWebView(songMeta, WebViewUtils.CanHandleWebViewUrl);
        return !videoUri.IsNullOrEmpty();
    }
}
