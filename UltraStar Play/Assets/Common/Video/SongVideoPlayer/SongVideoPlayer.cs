using System;
using System.Collections.Generic;
using System.IO;
using LibVLCSharp;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;

public class SongVideoPlayer : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    private static readonly HashSet<string> ignoredVideoFiles = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        ignoredVideoFiles.Clear();
    }

    [InjectedInInspector]
    public VideoPlayer videoPlayer;

    /**
     * SongAudioPlayer to synchronize the video with.
     */
    [InjectedInInspector]
    public SongAudioPlayer songAudioPlayer;

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

    private MediaPlayer vlcMediaPlayer;
    private Texture2D vlcTexture;
    private bool vlcFlipHorizontal = true;
    private bool vlcFlipVertical = true;

    public bool forceSyncOnForwardJumpInTheSong;

    private SongMeta loadedSongMeta;

    public bool IsLoaded => VideoSupportProvider is not EVideoSupportProvider.None;
    public bool IsFullyLoaded => IsLoaded && DurationInMillis > 0;

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

            if (VideoSupportProvider is EVideoSupportProvider.Vlc
                && vlcMediaPlayer != null
                && !IsVlcMediaPlayerFromSongAudioPlayer)
            {
                // TODO: Using MediaPlayer.SetRate makes the video stutter somehow
                // playbackSpeed = value;
                // vlcMediaPlayer.SetRate(playbackSpeed);
                // Debug.Log($"vlc setRate: {value}");
            }
            else if (VideoSupportProvider is EVideoSupportProvider.Ffmpeg)
            {
                // TODO: set playback speed to synchronize with audio
            }
            else if (VideoSupportProvider is EVideoSupportProvider.WebView)
            {
                // WebView is handled by the SongAudioPlayer exclusively.
                return;
            }
            else  if (VideoSupportProvider is EVideoSupportProvider.UnityVideoPlayer)
            {
                playbackSpeed = value;
                videoPlayer.playbackSpeed = playbackSpeed;
            }
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
            if (videoPlayer == null
                || !IsLoaded)
            {
                return 0;
            }

            if (VideoSupportProvider is EVideoSupportProvider.Vlc
                && vlcMediaPlayer != null)
            {
                return vlcMediaPlayer.Time;
            }
            else if (VideoSupportProvider is EVideoSupportProvider.Ffmpeg)
            {
                // Video is handled together with audio by the SongAudioPlayer via ffmpeg
                return songAudioPlayer.PositionInSongInMillis;
            }
            else if (VideoSupportProvider is EVideoSupportProvider.WebView)
            {
                // Video is handled together with audio by the SongAudioPlayer via WebViewManager
                return webViewManager.EstimatedPlaybackPositionInMillis;
            }
            else  if (VideoSupportProvider is EVideoSupportProvider.UnityVideoPlayer)
            {
                return videoPlayer.time * 1000;
            }

            return 0;
        }

        set
        {
            if (videoPlayer == null
                || !IsLoaded
                || double.IsNaN(value))
            {
                return;
            }

            double newPositionInVideoInMillis = value;
            double newPositionInVideoInSeconds = newPositionInVideoInMillis / 1000.0;

            if (VideoSupportProvider is EVideoSupportProvider.Vlc
                && vlcMediaPlayer != null
                && vlcMediaPlayer != songAudioPlayer.VlcMediaPlayer)
            {
                vlcMediaPlayer.SetTime((long)newPositionInVideoInMillis);
            }
            else if (VideoSupportProvider is EVideoSupportProvider.Ffmpeg)
            {
                // Video is handled together with audio by the SongAudioPlayer via ffmpeg
                return;
            }
            else if (VideoSupportProvider is EVideoSupportProvider.WebView)
            {
                // Video is handled together with audio by the SongAudioPlayer via WebViewManager
                return;
            }
            else  if (VideoSupportProvider is EVideoSupportProvider.UnityVideoPlayer)
            {
                videoPlayer.time = newPositionInVideoInSeconds;
            }
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
            if (VideoSupportProvider is EVideoSupportProvider.UnityVideoPlayer)
            {
                videoPlayer.isLooping = value;
            }
        }
    }

    private bool isPlaying;
    public bool IsPlaying => isPlaying;

    private bool IsVlcMediaPlayerFromSongAudioPlayer => vlcMediaPlayer != null
                                                    && vlcMediaPlayer == songAudioPlayer.VlcMediaPlayer;

    private RenderTexture originalWebViewCameraRenderTexture;
    public EVideoSupportProvider VideoSupportProvider { get; private set; } = EVideoSupportProvider.None;

    private readonly List<string> videoPlayerErrorMessages = new List<string>();

    public void OnInjectionFinished()
    {
        originalWebViewCameraRenderTexture = webViewManager.webViewCamera.targetTexture;
        HasLoadedBackgroundImage = false;
        InitEventSubscriber();
        UnloadVideo();

        sceneNavigator.BeforeSceneChangeEventStream
            .Subscribe(_ => ResetWebViewRenderTexture())
            .AddTo(gameObject);

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

    void Update()
    {
        if (!IsFullyLoaded)
        {
            return;
        }

        ApplyPlaybackStateToVideoProvider();

        if (songAudioPlayer != null)
        {
            SyncVideoWithMusic(false);
        }
        else
        {
            Debug.Log("no audio player");
        }

        if (VideoSupportProvider == EVideoSupportProvider.Vlc
            && vlcMediaPlayer != null
            && vlcMediaPlayer.IsPlaying)
        {
            UpdateVlcTextures();
        }
    }

    private void UpdateVlcTextures()
    {
        try
        {
            VlcManager.UpdateVlcTextures(vlcMediaPlayer, ref vlcTexture);

            if (vlcMediaPlayer != null
                && vlcTexture != null)
            {
                IntPtr texPtr = vlcMediaPlayer.GetTexture((uint)vlcTexture.width, (uint)vlcTexture.height, out bool updated);
                if (updated)
                {
                    vlcTexture.UpdateExternalTexture(texPtr);

                    // Copy the vlc texture into the target RenderTexture
                    Vector2 scale = new Vector2(vlcFlipHorizontal ? -1 : 1, vlcFlipVertical ? -1 : 1);
                    Graphics.Blit(vlcTexture, videoPlayer.targetTexture, scale, Vector2.zero);
                }
            }
        }
        catch (VLCException ex)
        {
            Debug.LogWarning($"Failed to update VLC textures: {ex.Message}");
        }
    }

    private IObservable<SongVideoLoadedEvent> LoadAndPlayVideoAsObservable(SongMeta songMeta, string videoUri)
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
            return ObservableUtils.LogErrorThenThrow<SongVideoLoadedEvent>(
                new SongAudioPlayerException($"Unsupported video resource '{videoUri}'."));
        }
    }

    private IObservable<SongVideoLoadedEvent> LoadWithVlc(SongMeta songMeta, string videoUri)
    {
        Debug.Log($"SongVideoPlayer loading video via vlc: '{videoUri}'");

        ResetWebViewRenderTexture();
        ResetFfmpegRenderTexture();

        if (songMeta.Video == songMeta.Mp3
            && songAudioPlayer.VlcMediaPlayer != null)
        {
            // Use VLC MediaPlayer of SongAudioPlayer
            Debug.Log("SongVideoPlayer - Using VLC MediaPlayer instance of SongAudioPlayer because audio and video resource is the same");

            // Destroy old instance if any
            DestroyVlcMediaPlayer();

            // Use instance of SongAudioPlayer
            vlcMediaPlayer = songAudioPlayer.VlcMediaPlayer;
            VideoSupportProvider = EVideoSupportProvider.Vlc;
            return Observable.Create<SongVideoLoadedEvent>(o =>
            {
                DurationInMillis = (int)vlcMediaPlayer.Length;
                PlayAndFireLoadedEvent(o, songMeta, videoUri);
                return Disposable.Empty;
            });
        }

        try
        {
            // Instantiate new vlc player
            if (vlcMediaPlayer == null
                || IsVlcMediaPlayerFromSongAudioPlayer)
            {
                vlcMediaPlayer = vlcManager.CreateMediaPlayer();
                vlcManager.DisableMediaPlayerAudioOutput(vlcMediaPlayer);
            }

            if (vlcMediaPlayer.Media != null)
            {
                vlcMediaPlayer.Media.Dispose();
            }

            vlcMediaPlayer.Media = new Media(new Uri(videoUri));
            vlcMediaPlayer.Play();

            VideoSupportProvider = EVideoSupportProvider.Vlc;
        }
        catch (Exception e)
        {
            try
            {
                DestroyVlcMediaPlayer();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to destroy vlc player after failing to load '{videoUri}' using vlc");
            }

            Debug.LogException(e);
            Debug.LogError($"Failed to load '{videoUri}' using vlc");
            return ObservableUtils.LogErrorThenThrow<SongVideoLoadedEvent>(
                new SongAudioPlayerException($"Failed to load '{videoUri}'"));
        }

        // The video is loaded asynchronously.
        // The duration property indicates whether it has been loaded.
        return Observable.Create<SongVideoLoadedEvent>(o =>
        {
            StartCoroutine(CoroutineUtils.ExecuteWhenConditionIsTrue(
                () => vlcMediaPlayer.Media != null && vlcMediaPlayer.Media.Duration > 0,
                () =>
                {
                    if (vlcMediaPlayer != songAudioPlayer.VlcMediaPlayer)
                    {
                        vlcMediaPlayer.SetTime(songAudioPlayer.VlcMediaPlayer.Time);
                    }

                    DurationInMillis = (int)vlcMediaPlayer.Length;
                    PlayAndFireLoadedEvent(o, songMeta, videoUri);
                }));
            return Disposable.Empty;
        });
    }

    private IObservable<SongVideoLoadedEvent> LoadWithFfmpeg(SongMeta songMeta, string videoUri)
    {
        Debug.Log($"SongVideoPlayer loading video via ffmpeg: '{videoUri}'");

        VideoSupportProvider = EVideoSupportProvider.Ffmpeg;
        ResetWebViewRenderTexture();
        SetFfmpegRenderTextureToVideoRenderTexture();

        return Observable.Create<SongVideoLoadedEvent>(o =>
        {
            DurationInMillis = songAudioPlayer.DurationOfSongInMillis;
            PlayAndFireLoadedEvent(o, songMeta, videoUri);
            return Disposable.Empty;
        });
    }

    private IObservable<SongVideoLoadedEvent> LoadWithWebView(SongMeta songMeta, string videoUri)
    {
        VideoSupportProvider = EVideoSupportProvider.WebView;
        ResetFfmpegRenderTexture();
        SetWebViewRenderTextureToVideoRenderTexture();

        return Observable.Create<SongVideoLoadedEvent>(o =>
        {
            DurationInMillis = songAudioPlayer.DurationOfSongInMillis;
            PlayAndFireLoadedEvent(o, songMeta, videoUri);
            return Disposable.Empty;
        });
    }

    private IObservable<SongVideoLoadedEvent> LoadWithVideoPlayer(SongMeta songMeta, string uri)
    {
        VideoSupportProvider = EVideoSupportProvider.UnityVideoPlayer;
        ResetFfmpegRenderTexture();
        ResetWebViewRenderTexture();

        videoPlayer.url = ApplicationUtils.GetVideoPlayerUri(uri);
        if (videoPlayer.url.IsNullOrEmpty())
        {
            // The url is empty if loading the video failed.
            VideoSupportProvider = EVideoSupportProvider.None;
            return ObservableUtils.LogErrorThenThrow<SongVideoLoadedEvent>(new SongVideoPlayerException($"Unable to load video '{uri}' with Unity's VideoPlayer"));
        }

        // Start VideoPlayer to trigger loading
        videoPlayer.Play();

        // The video is loaded asynchronously. The length property of the VideoPlayer indicates whether it has been loaded.
        return Observable.Create<SongVideoLoadedEvent>(o =>
        {
            StartCoroutine(CoroutineUtils.ExecuteWhenConditionIsTrue(
                () => videoPlayer.length > 0 || videoPlayerErrorMessages.Count > 0,
                () =>
                {
                    if (videoPlayerErrorMessages.Count > 0)
                    {
                        UnloadVideo();
                        if (settings.VlcToPlayMediaFilesUsage
                            is EThirdPartyLibraryUsage.WhenUnsupportedByUnity
                            or EThirdPartyLibraryUsage.Always)
                        {
                            Debug.Log($"Trying to load video with vlc because Unity's VideoPlayer failed: '{uri}'");
                            LoadWithVlc(songMeta, uri)
                                .Subscribe(o.OnNext, o.OnError, o.OnCompleted);
                        }
                        else if (settings.FfmpegToPlayMediaFilesUsage
                                     is EThirdPartyLibraryUsage.WhenUnsupportedByUnity
                                     or EThirdPartyLibraryUsage.Always
                                 && string.Equals(songMeta.Mp3, songMeta.Video, StringComparison.InvariantCultureIgnoreCase))
                        {
                            Debug.Log($"Trying to load video with ffmpeg because Unity's VideoPlayer failed: '{uri}'");
                            LoadWithFfmpeg(songMeta, uri)
                                .Subscribe(o.OnNext, o.OnError, o.OnCompleted);
                        }
                        else if (settings.FfmpegToPlayMediaFilesUsage
                                 is EThirdPartyLibraryUsage.WhenUnsupportedByUnity
                                 or EThirdPartyLibraryUsage.Always)
                        {
                            Debug.LogError($"Failed to load video with Unity's VideoPlayer and cannot use ffmpeg because the video and audio resource are not equal. Video URI: '{uri}', Video: '{songMeta.Video}', Audio URI: '{songMeta.Mp3}'");
                        }
                        return;
                    }

                    DurationInMillis = videoPlayer.length * 1000.0;
                    videoPlayer.time = songAudioPlayer.PositionInSongInSeconds;
                    PlayAndFireLoadedEvent(o, songMeta, uri);
                }));
            return Disposable.Empty;
        });
    }

    private void PlayAndFireLoadedEvent(IObserver<SongVideoLoadedEvent> o, SongMeta songMeta, string uri)
    {
        Play();

        o.OnNext(new SongVideoLoadedEvent(songMeta, uri));
        loadedEventStream.OnNext(new SongVideoLoadedEvent(songMeta, uri));
    }

    private void ShowVideoImageVisualElement()
    {
        if (videoImageVisualElement != null)
        {
            videoImageVisualElement.ShowByDisplay();
            videoImageVisualElement.style.opacity = 1;
        }
    }

    private void SetWebViewRenderTextureToVideoRenderTexture()
    {
        if (originalWebViewCameraRenderTexture == null)
        {
            return;
        }

        webViewManager.SetWebViewRenderTexture(videoPlayer.targetTexture);
    }

    private void ResetWebViewRenderTexture()
    {
        if (originalWebViewCameraRenderTexture == null)
        {
            return;
        }

        if (webViewManager.webViewCamera.targetTexture != originalWebViewCameraRenderTexture)
        {
            webViewManager.webViewCamera.targetTexture = originalWebViewCameraRenderTexture;
        }
    }

    private void SetFfmpegRenderTextureToVideoRenderTexture()
    {
        songAudioPlayer.FfmpegRenderTexture = videoPlayer.targetTexture;
    }

    private void ResetFfmpegRenderTexture()
    {
        songAudioPlayer.FfmpegRenderTexture = null;
    }

    private void UnloadVideo()
    {
        StopAllCoroutines();
        videoPlayerErrorMessages.Clear();

        if (VideoSupportProvider is EVideoSupportProvider.Vlc
            && vlcMediaPlayer != null
            && vlcMediaPlayer != songAudioPlayer.VlcMediaPlayer)
        {
            vlcMediaPlayer.Stop();
        }
        else if (VideoSupportProvider is EVideoSupportProvider.Ffmpeg)
        {
            // Handled by SongAudioPlayer
        }
        else if (VideoSupportProvider is EVideoSupportProvider.UnityVideoPlayer)
        {
            videoPlayer.Stop();
            videoPlayer.clip = null;
            videoPlayer.source = VideoSource.VideoClip;
        }
        RenderTextureUtils.Clear(videoPlayer.targetTexture);
        VideoSupportProvider = EVideoSupportProvider.None;

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
            Pause();
        }
        else if (songAudioPlayerIsPlaying
                 && !IsPlaying)

        {
            if (!IsWaitingForVideoGap(songAudioPlayer.PositionInSongInMillis, loadedSongMeta.VideoGap * 1000))
            {
                Play();
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
        if (IsWaitingForVideoGap(positionInAudioInMillis, loadedSongMeta.VideoGap * 1000))
        {
            return;
        }

        // Loop short videos
        IsLooping = DurationInMillis < durationOfAudioInMillis / 2;

        // Both, the smooth sync and immediate sync need some time.
        nextSyncTimeInSeconds = Time.time + 1;

        double targetPositionInVideoInMillis = (loadedSongMeta.VideoGap * 1000) + positionInAudioInMillis;
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

        if (videoImageVisualElement != null)
        {
            videoImageVisualElement.HideByDisplay();
            videoImageVisualElement.style.opacity = 0;
        }
        if (songMeta.Background.IsNullOrEmpty())
        {
            ShowCoverImageAsBackground(songMeta);
            return;
        }

        string backgroundUri = SongMetaUtils.GetBackgroundUri(songMeta);
        if (!SongMetaUtils.BackgroundResourceExists(songMeta))
        {
            Debug.LogWarning("Showing cover image because background image resource does not exist: " + backgroundUri);
            ShowCoverImageAsBackground(songMeta);
            return;
        }

        LoadBackgroundImage(backgroundUri);
    }

    private void ShowCoverImageAsBackground(SongMeta songMeta)
    {
        string coverUri = SongMetaUtils.GetCoverUri(songMeta);
        if (coverUri.IsNullOrEmpty())
        {
            return;
        }

        if (!SongMetaUtils.CoverResourceExists(songMeta))
        {
            Debug.LogWarning("Cover image resource does not exist: " + coverUri);
            return;
        }

        LoadBackgroundImage(coverUri);
    }

    private void LoadBackgroundImage(string backgroundUri)
    {
        if (backgroundUri.IsNullOrEmpty())
        {
            return;
        }

        ImageManager.LoadSpriteFromUri(backgroundUri, loadedSprite =>
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
        LoadAndPlaySongVideoAsObservable(songMeta)
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to load video of '{SongMetaUtils.GetArtistDashTitle(songMeta)}': {ex.Message}");
                ShowBackgroundImage(songMeta);
            })
            // Subscribe to trigger observable
            .Subscribe(evt =>
            {
                Debug.Log($"Successfully loaded video of song '{SongMetaUtils.GetArtistDashTitle(songMeta)}'");

                if (loadedSongMeta.VideoGap > 0)
                {
                    // Positive VideoGap, thus skip the start of the video
                    PositionInVideoInSeconds = loadedSongMeta.VideoGap;
                }

                ShowVideoImageVisualElement();
            });
    }

    public IObservable<SongVideoLoadedEvent> LoadAndPlaySongVideoAsObservable(SongMeta songMeta)
    {
        UnloadVideo();

        loadedSongMeta = songMeta;

        // Use the audio URL as video if the WebView can handle it (e.g. a YouTube video).
        string videoUri = SongMetaUtils.GetVideoUriPreferAudioUriIfWebView(songMeta, WebViewUtils.CanHandleWebViewUrl);

        if (videoUri.IsNullOrEmpty())
        {
            return ObservableUtils.LogErrorThenThrow<SongVideoLoadedEvent>(
                new SongVideoPlayerException($"Ignoring empty video resource"));
        }

        if (ignoredVideoFiles.Contains(songMeta.Video))
        {
            return ObservableUtils.LogErrorThenThrow<SongVideoLoadedEvent>(
                new SongVideoPlayerException($"Ignoring video resource: '{videoUri}'"));
        }

        if (!SongMetaUtils.ResourceExists(songMeta, videoUri))
        {
            return ObservableUtils.LogErrorThenThrow<SongVideoLoadedEvent>(
                new SongVideoPlayerException($"Video resource does not exist: {videoUri}"));
        }

        return LoadAndPlayVideoAsObservable(songMeta, videoUri);
    }

    void OnEnable()
    {
        videoPlayer.errorReceived += OnVideoPlayerErrorReceived;
    }

    void OnDisable()
    {
        videoPlayer.errorReceived -= OnVideoPlayerErrorReceived;
    }

    private void DestroyVlcMediaPlayer()
    {
        if (vlcMediaPlayer == null
            // SongAudioPlayer may also take care of destroying the instance
            || vlcMediaPlayer == songAudioPlayer.VlcMediaPlayer)
        {
            return;
        }

        VlcManager.DestroyMediaPlayer(vlcMediaPlayer);
        vlcMediaPlayer = null;
    }

    private void OnDestroy()
    {
        RenderTextureUtils.Clear(videoPlayer.targetTexture);
        Destroy(vlcTexture);
        DestroyVlcMediaPlayer();
    }

    private void OnVideoPlayerErrorReceived(VideoPlayer source, string message)
    {
        Debug.LogError($"SongVideoPlayer received VideoPlayer error: {message}");
        videoPlayerErrorMessages.Add(message);
    }

    private void UpdateBackgroundScaleMode()
    {
        switch (settings.SongBackgroundScaleMode)
        {
            case ESongBackgroundScaleMode.FitInside:
                if (videoPlayer != null)
                {
                    videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
                }
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
                if (videoPlayer != null)
                {
                    videoPlayer.aspectRatio = VideoAspectRatio.FitOutside;
                }
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

        if (VideoSupportProvider is EVideoSupportProvider.Vlc
            && vlcMediaPlayer != null
            && !IsVlcMediaPlayerFromSongAudioPlayer)
        {
            if (IsPlaying
                && !vlcMediaPlayer.IsPlaying)
            {
                Debug.Log("SongVideoPlayer should be playing, but vlcMediaPlayer is not. Starting its playback now.");
                vlcMediaPlayer.Play();
            }
            else if (!IsPlaying
                     && vlcMediaPlayer.IsPlaying)
            {
                Debug.Log("SongVideoPlayer should not be playing, but vlcMediaPlayer is. Pausing its playback now.");
                vlcMediaPlayer.Pause();
            }
        }
        else if (VideoSupportProvider is EVideoSupportProvider.Ffmpeg)
        {
            // Handled by SongAudioPlayer
        }
        else if (VideoSupportProvider is EVideoSupportProvider.WebView)
        {
            // Handled by SongAudioPlayer
        }
        else if (VideoSupportProvider is EVideoSupportProvider.UnityVideoPlayer
                 && videoPlayer != null)
        {
            if (IsPlaying
                && !videoPlayer.isPlaying)
            {
                Debug.Log("SongVideoPlayer should be playing, but Unity VideoPlayer is not. Starting its playback now.");
                videoPlayer.Play();
            }
            else if (!IsPlaying
                     && videoPlayer.isPlaying)
            {
                Debug.Log("SongVideoPlayer should not be playing, but Unity VideoPlayer is. Pausing its playback now.");
                videoPlayer.Pause();
            }
        }
    }

    private void Play()
    {
        SetPlaying(true);
    }

    private void Pause()
    {
        SetPlaying(false);
    }

    private void SetPlaying(bool value)
    {
        isPlaying = value;
        if (VideoSupportProvider is EVideoSupportProvider.Vlc
            && vlcMediaPlayer != null
            && !IsVlcMediaPlayerFromSongAudioPlayer)
        {
            if (isPlaying)
            {
                vlcMediaPlayer.Play();
            }
            else
            {
                vlcMediaPlayer.Pause();
            }
        }
        else if (VideoSupportProvider is EVideoSupportProvider.Ffmpeg)
        {
            // Handled by SongAudioPlayer
            return;
        }
        else if (VideoSupportProvider is EVideoSupportProvider.WebView)
        {
            // Handled by SongAudioPlayer
            return;
        }
        else if (VideoSupportProvider is EVideoSupportProvider.UnityVideoPlayer)
        {
            if (isPlaying)
            {
                videoPlayer.Play();
            }
            else
            {
                videoPlayer.Pause();
            }
        }
    }

    public static void AddIgnoredVideoFile(string uri)
    {
        ignoredVideoFiles.Add(uri);
    }
}
