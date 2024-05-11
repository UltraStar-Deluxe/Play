using System;
using System.Collections.Generic;
using System.IO;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class SongVideoPlayer : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    private static readonly HashSet<string> ignoredVideoFiles = new();

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
    public UnityVideoPlayerVideoSupportProvider unityVideoPlayerVideoSupportProvider;

    [InjectedInInspector]
    public AbstractVideoSupportProvider vlcVideoSupportProvider;

    [InjectedInInspector]
    public AbstractVideoSupportProvider songAudioPlayerVlcVideoSupportProvider;

    [InjectedInInspector]
    public AbstractVideoSupportProvider ffmpegVideoSupportProvider;

    [InjectedInInspector]
    public AbstractVideoSupportProvider webViewVideoSupportProvider;

    private IVideoSupportProvider currentVideoSupportProvider;

    private List<IVideoSupportProvider> VideoSupportProviders => new()
    {
        unityVideoPlayerVideoSupportProvider,
        vlcVideoSupportProvider,
        songAudioPlayerVlcVideoSupportProvider,
        ffmpegVideoSupportProvider,
        webViewVideoSupportProvider,
    };

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
            if (IsLoaded
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

    public bool forceSyncOnForwardJumpInTheSong;

    private SongMeta loadedSongMeta;

    public bool IsLoaded => VideoSupportProvider is not EVideoSupportProvider.None;
    public bool IsFullyLoaded => IsLoaded && DurationInMillis > 0 && loadedSongMeta != null;

    public double DurationInMillis { get; private set; }

    public bool HasLoadedBackgroundImage { get; private set; }

    private float playbackSpeed = 1;
    public float PlaybackSpeed
    {
        get => playbackSpeed;
        set
        {
            if (!IsFullyLoaded
                || float.IsNaN(value))
            {
                return;
            }

            currentVideoSupportProvider.PlaybackSpeed = value;
        }
    }

    public double PositionInVideoInSeconds
    {
        get => PositionInVideoInMillis / 1000.0;
        set => PositionInVideoInMillis = value * 1000.0;
    }

    public double PositionInVideoInMillis
    {
        get
        {
            if (!IsLoaded)
            {
                return 0;
            }

            if (UseVlcMediaPlayerOfSongAudioPlayer)
            {
                return songAudioPlayer.VlcMediaPlayer.Time;
            }
            return currentVideoSupportProvider.PositionInVideoInMillis;
        }

        set
        {
            if (!IsLoaded
                || double.IsNaN(value)
                || UseVlcMediaPlayerOfSongAudioPlayer)
            {
                return;
            }

            currentVideoSupportProvider.PositionInVideoInMillis = value;
        }
    }

    private readonly Subject<SongVideoLoadedEvent> loadedEventStream = new();
    public IObservable<SongVideoLoadedEvent> LoadedEventStream => loadedEventStream;

    private float nextSyncTimeInSeconds;

    private IDisposable jumpBackInSongEventStreamDisposable;
    private IDisposable jumpForwardInSongEventStreamDisposable;

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
            isLooping = value;
            currentVideoSupportProvider.IsLooping = value;
        }
    }

    private bool isPlaying;
    public bool IsPlaying => isPlaying;

    private bool IsPlayingOfVideoProvider =>
        (UseVlcMediaPlayerOfSongAudioPlayer && songAudioPlayer.VlcMediaPlayer.IsPlaying)
        || currentVideoSupportProvider.IsPlaying;

    private bool UseVlcMediaPlayerOfSongAudioPlayer => VideoSupportProvider is EVideoSupportProvider.Vlc
        && loadedSongMeta != null
        && loadedSongMeta.Audio == loadedSongMeta.Video
        && songAudioPlayer.VlcMediaPlayer != null;

    public EVideoSupportProvider VideoSupportProvider => currentVideoSupportProvider?.VideoSupportProvider ?? EVideoSupportProvider.None;

    private float lastApplyPlaybackStateToVideoProviderTimeInSeconds;

    public void OnInjectionFinished()
    {
        currentVideoSupportProvider = unityVideoPlayerVideoSupportProvider;

        HasLoadedBackgroundImage = false;
        InitEventSubscriber();
        UnloadVideo();

        settings.ObserveEveryValueChanged(it => it.SongBackgroundScaleMode)
            .Subscribe(_ => UpdateBackgroundScaleMode())
            .AddTo(gameObject);
    }

    private void InitEventSubscriber()
    {
        // Jump backward in song
        if (jumpBackInSongEventStreamDisposable != null)
        {
            jumpBackInSongEventStreamDisposable.Dispose();
        }
        jumpBackInSongEventStreamDisposable = songAudioPlayer.JumpBackInSongEventStream
            .Subscribe(evt =>
            {
                if (Math.Abs(evt.Previous - evt.Current) > 400)
                {
                    SyncVideoWithMusic(true);
                }
            })
            .AddTo(gameObject);

        // Jump forward in song
        if (jumpForwardInSongEventStreamDisposable != null)
        {
            jumpForwardInSongEventStreamDisposable.Dispose();
        }
        if (forceSyncOnForwardJumpInTheSong)
        {
            jumpForwardInSongEventStreamDisposable = songAudioPlayer.JumpForwardInSongEventStream
                .Subscribe(evt =>
                {
                    if (Math.Abs(evt.Previous - evt.Current) > 400)
                    {
                        SyncVideoWithMusic(true);
                    }
                })
                .AddTo(gameObject);
        }
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
                && PositionInVideoInMillis < DurationInMillis - 100)
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

    private IObservable<VideoLoadedEvent> LoadAndPlayVideoAsObservable(SongMeta songMeta, string videoUri)
    {
        if (WebViewUtils.CanHandleWebViewUrl(videoUri))
        {
            return LoadWithWebView(songMeta, videoUri);
        }

        string videoFileExtension = Path.GetExtension(videoUri);
        if (ApplicationUtils.IsUnitySupportedVideoFormat(videoFileExtension))
        {
            if (settings.VlcToPlayMediaFilesUsage is EThirdPartyLibraryUsage.Always)
            {
                return LoadWithVlc(songMeta, videoUri);
            }
            else if (settings.FfmpegToPlayMediaFilesUsage is EThirdPartyLibraryUsage.Always)
            {
                return LoadWithFfmpeg(songMeta, videoUri);
            }
            else
            {
                return LoadWithVideoPlayer(songMeta, videoUri);
            }
        }
        else if (settings.VlcToPlayMediaFilesUsage
                 is EThirdPartyLibraryUsage.WhenUnsupportedByUnity
                 or EThirdPartyLibraryUsage.Always)
        {
            return LoadWithVlc(songMeta, videoUri);
        }
        else if (settings.FfmpegToPlayMediaFilesUsage
                 is EThirdPartyLibraryUsage.WhenUnsupportedByUnity
                 or EThirdPartyLibraryUsage.Always)
        {
            return LoadWithFfmpeg(songMeta, videoUri);
        }
        else
        {
            return ObservableUtils.LogExceptionThenThrow<VideoLoadedEvent>(
                new SongAudioPlayerException($"Unsupported video resource '{videoUri}'."));
        }
    }

    private IObservable<VideoLoadedEvent> LoadWithVlc(SongMeta songMeta, string videoUri)
    {
        Debug.Log($"SongVideoPlayer loading video via vlc: '{videoUri}'");
        UnloadVideo();
        loadedSongMeta = songMeta;

        if (songMeta.Video == songMeta.Audio
            && songAudioPlayer.VlcMediaPlayer != null)
        {
            // Use VLC MediaPlayer of SongAudioPlayer
            Debug.Log("SongVideoPlayer - Using VLC MediaPlayer instance of SongAudioPlayer because audio and video resource is the same");

            currentVideoSupportProvider = songAudioPlayerVlcVideoSupportProvider;
            UpdateVideoSupportProviderTargetTexture();
            return songAudioPlayerVlcVideoSupportProvider.LoadVideoAsObservable(videoUri);
        }

        currentVideoSupportProvider = vlcVideoSupportProvider;
        UpdateVideoSupportProviderTargetTexture();
        return vlcVideoSupportProvider.LoadVideoAsObservable(videoUri);
    }

    private IObservable<VideoLoadedEvent> LoadWithFfmpeg(SongMeta songMeta, string videoUri)
    {
        Debug.Log($"SongVideoPlayer loading video via ffmpeg: '{videoUri}'");
        UnloadVideo();
        loadedSongMeta = songMeta;

        currentVideoSupportProvider = ffmpegVideoSupportProvider;
        UpdateVideoSupportProviderTargetTexture();

        return ffmpegVideoSupportProvider.LoadVideoAsObservable(videoUri);
    }

    private IObservable<VideoLoadedEvent> LoadWithWebView(SongMeta songMeta, string videoUri)
    {
        Debug.Log($"SongVideoPlayer loading video via WebView: '{videoUri}'");
        UnloadVideo();
        loadedSongMeta = songMeta;

        currentVideoSupportProvider = webViewVideoSupportProvider;
        UpdateVideoSupportProviderTargetTexture();
        return currentVideoSupportProvider.LoadVideoAsObservable(videoUri);
    }

    private IObservable<VideoLoadedEvent> LoadWithVideoPlayer(SongMeta songMeta, string videoUri)
    {
        Debug.Log($"SongVideoPlayer loading video via Unity VideoPlayer: '{videoUri}'");
        UnloadVideo();
        loadedSongMeta = songMeta;

        currentVideoSupportProvider = unityVideoPlayerVideoSupportProvider;
        return Observable.Create<VideoLoadedEvent>(o =>
        {
            unityVideoPlayerVideoSupportProvider.LoadVideoAsObservable(ApplicationUtils.GetVideoPlayerUri(videoUri))
                .CatchIgnore((Exception ex) =>
                {
                    UnloadVideo();

                    if (settings.VlcToPlayMediaFilesUsage
                        is EThirdPartyLibraryUsage.WhenUnsupportedByUnity
                        or EThirdPartyLibraryUsage.Always)
                    {
                        Debug.Log($"Trying to load video with vlc because Unity's VideoPlayer failed: '{videoUri}'");
                        LoadWithVlc(songMeta, videoUri)
                            .Subscribe(o.OnNext, o.OnError, o.OnCompleted);
                    }
                    else if (settings.FfmpegToPlayMediaFilesUsage
                                 is EThirdPartyLibraryUsage.WhenUnsupportedByUnity
                                 or EThirdPartyLibraryUsage.Always
                             && string.Equals(songMeta.Audio, songMeta.Video,
                                 StringComparison.InvariantCultureIgnoreCase))
                    {
                        Debug.Log($"Trying to load video with ffmpeg because Unity's VideoPlayer failed: '{videoUri}'");
                        LoadWithFfmpeg(songMeta, videoUri)
                            .Subscribe(o.OnNext, o.OnError, o.OnCompleted);
                    }
                    else if (settings.FfmpegToPlayMediaFilesUsage
                             is EThirdPartyLibraryUsage.WhenUnsupportedByUnity
                             or EThirdPartyLibraryUsage.Always)
                    {
                        Debug.LogError(
                            $"Failed to load video with Unity's VideoPlayer and cannot use ffmpeg because the video and audio resource are not equal. Video URI: '{videoUri}', Video: '{songMeta.Video}', Audio URI: '{songMeta.Audio}'");
                    }
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

        // VideoSupportProviders.ForEach(videoSupportProvider => videoSupportProvider.UnloadVideo());
        currentVideoSupportProvider.UnloadVideo();
        currentVideoSupportProvider = unityVideoPlayerVideoSupportProvider;
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
                && DurationInMillis <= songAudioPlayer.PositionInSongInSeconds
                && !IsLooping)
            || FreezeVideo)
        {
            PauseVideo();
        }
        else if (songAudioPlayerIsPlaying
                 && !IsPlaying)

        {
            if (!IsWaitingForVideoGap(songAudioPlayer.PositionInSongInMillis, loadedSongMeta.VideoGapInMillis))
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

        double positionInAudioInMillis = songAudioPlayer.PositionInSongInMillis;
        double durationOfAudioInMillis = songAudioPlayer.DurationOfSongInMillis;
        if (IsWaitingForVideoGap(positionInAudioInMillis, loadedSongMeta.VideoGapInMillis))
        {
            return;
        }

        // Loop short videos
        IsLooping = DurationInMillis < durationOfAudioInMillis / 2;

        // Both, the smooth sync and immediate sync need some time.
        nextSyncTimeInSeconds = Time.time + 1;

        double targetPositionInVideoInMillis = (loadedSongMeta.VideoGapInMillis) + positionInAudioInMillis;
        if (IsLooping)
        {
            targetPositionInVideoInMillis %= DurationInMillis;
        }
        double timeDifferenceInMillis = targetPositionInVideoInMillis - PositionInVideoInMillis;

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
                PositionInVideoInMillis = targetPositionInVideoInMillis;
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
    private bool IsWaitingForVideoGap(double positionInSongInMillis, double videoGapInMillis)
    {
        // A negative video gap means this duration has to be waited before playing the video.
        return videoGapInMillis < 0 && positionInSongInMillis < -videoGapInMillis;
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
        LoadAndPlaySongVideoAsObservable(loadedSongMeta)
            .CatchIgnore((Exception ex) => Debug.LogException(ex))
            // Subscribe to trigger the observable
            .Subscribe(evt => Debug.Log($"Loaded video: {evt.VideoUri}"));
    }

    public void LoadAndPlaySongVideoOrShowBackgroundImage(SongMeta songMeta)
    {
        if (!HasVideoUri(songMeta)
            || IsSongVideoPlaybackDisabled())
        {
            ShowBackgroundImage(songMeta);
            return;
        }

        LoadAndPlaySongVideoAsObservable(songMeta)
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to load video of '{songMeta.GetArtistDashTitle()}': {ex.Message}");
                ShowBackgroundImage(songMeta);
            })
            // Subscribe to trigger observable
            .Subscribe(evt =>
            {
                Debug.Log($"Successfully loaded video of song '{songMeta.GetArtistDashTitle()}'");

                if (loadedSongMeta.VideoGapInMillis > 0)
                {
                    // Positive VideoGap, thus skip the start of the video
                    PositionInVideoInMillis = loadedSongMeta.VideoGapInMillis;
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

    public IObservable<SongVideoLoadedEvent> LoadAndPlaySongVideoAsObservable(SongMeta songMeta)
    {
        UnloadVideo();

        loadedSongMeta = songMeta;

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

        return LoadAndPlayVideoAsObservable(songMeta, videoUri)
            .Select(evt =>
            {
                currentVideoSupportProvider.PositionInVideoInMillis = songAudioPlayer.PositionInSongInMillis;
                DurationInMillis = currentVideoSupportProvider.DurationInMillis;
                PlayVideo();
                loadedEventStream.OnNext(new SongVideoLoadedEvent(songMeta, videoUri));
                return new SongVideoLoadedEvent(songMeta, videoUri);
            });
    }

    private void UpdateBackgroundScaleMode()
    {
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
            Debug.Log($"SongVideoPlayer should be playing, but {currentVideoSupportProvider.GetType().Name} is not. Starting its playback now.");
            currentVideoSupportProvider.PlayVideo();
        }
        else if (!IsPlaying
                 && currentVideoSupportProvider.IsPlaying)
        {
            Debug.Log($"SongVideoPlayer should not be playing, but {currentVideoSupportProvider.GetType().Name} is. Pausing its playback now.");
            currentVideoSupportProvider.PauseVideo();
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
        currentVideoSupportProvider.StopVideo();
    }

    private void SetPlaying(bool value)
    {
        isPlaying = value;
        currentVideoSupportProvider.IsPlaying = value;
    }

    private void UpdateVideoSupportProviderTargetTexture()
    {
        currentVideoSupportProvider.SetTargetTexture(unityVideoPlayerVideoSupportProvider.videoPlayer.targetTexture);
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
