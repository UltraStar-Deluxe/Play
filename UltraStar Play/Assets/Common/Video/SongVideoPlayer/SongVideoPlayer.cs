using System;
using System.Collections.Generic;
using System.IO;
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
            if (HasLoadedVideo
                && videoImageVisualElement != null)
            {
                videoImageVisualElement.ShowByDisplay();
                videoImageVisualElement.style.opacity = 1;
            }
        }
    }

    [Inject(UxmlName = R.UxmlNames.songImage, Optional = true)]
    private VisualElement backgroundImageVisualElement;

    public bool forceSyncOnForwardJumpInTheSong;

    [Inject]
    private Settings settings;
    
    [Inject]
    private SceneNavigator sceneNavigator;

    private SongMeta songMeta;
    public SongMeta SongMeta
    {
        get
        {
            return songMeta;
        }

        set
        {
            songMeta = value;
            LoadSongVideoAsObservable(songMeta)
                // Subscribe to trigger the observable
                .CatchIgnore((Exception ex) => Debug.LogException(ex))
                .Subscribe(evt => Debug.Log($"Loaded video: {evt.VideoUri}"));
        }
    }

    public bool HasLoadedVideo => VideoSupportProvider is not EVideoSupportProvider.None;
    public bool HasLoadedBackgroundImage { get; private set; }

    public float PlaybackSpeed
    {
        get
        {
            if (videoPlayer == null
                || !HasLoadedVideo)
            {
                return 0;
            }

            if (VideoSupportProvider is EVideoSupportProvider.WebView)
            {
                return 1;
            }
            else if (VideoSupportProvider is EVideoSupportProvider.Ffmpeg)
            {
                return 1;
            }
            else  if (VideoSupportProvider is EVideoSupportProvider.UnityVideoPlayer)
            {
                return videoPlayer.playbackSpeed;
            }

            return 0;
        }

        set
        {
            if (videoPlayer == null
                || !HasLoadedVideo
                || float.IsNaN(value))
            {
                return;
            }

            if (VideoSupportProvider is EVideoSupportProvider.Ffmpeg)
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
                videoPlayer.playbackSpeed = value;
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
                || !HasLoadedVideo)
            {
                return 0;
            }

            if (VideoSupportProvider is EVideoSupportProvider.Ffmpeg)
            {
                // Video is handled by the SongAudioPlayer via ffmpeg
                return songAudioPlayer.PositionInSongInMillis;
            }
            else if (VideoSupportProvider is EVideoSupportProvider.WebView)
            {
                // WebView is handled by the SongAudioPlayer exclusively.
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
                || !HasLoadedVideo
                || double.IsNaN(value))
            {
                return;
            }

            double newPositionInVideoInMillis = value;
            double newPositionInVideoInSeconds = newPositionInVideoInMillis / 1000.0;

            if (VideoSupportProvider is EVideoSupportProvider.Ffmpeg)
            {
                // Video is handled by the SongAudioPlayer via ffmpeg
                return;
            }
            else if (VideoSupportProvider is EVideoSupportProvider.WebView)
            {
                // WebView is handled by the SongAudioPlayer exclusively.
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
        get => FreezeVideo;
        set
        {
            freezeVideo = value;
            SyncVideoWithMusic(false);
        }
    }
    
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
            .Subscribe(_ => SyncVideoWithMusic(true));

        // Jump forward in song
        if (jumpForwardInSongEventStreamDisposable != null)
        {
            jumpForwardInSongEventStreamDisposable.Dispose();
        }
        if (forceSyncOnForwardJumpInTheSong)
        {
            jumpForwardInSongEventStreamDisposable = songAudioPlayer.JumpForwardInSongEventStream
                .Subscribe(_ => SyncVideoWithMusic(true));
        }
    }

    void Update()
    {
        if (!HasLoadedVideo)
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
    }

    public void StartVideoOrShowBackgroundImage()
    {
        if (HasLoadedVideo)
        {
            StartVideoPlayback();
        }
        else
        {
            ShowBackgroundImage();
        }
    }

    private IObservable<SongVideoLoadedEvent> LoadVideoAsObservable(SongMeta localSongMeta, string uri)
    {
        if (WebViewUtils.CanHandleWebViewUrl(uri))
        {
            return LoadWithWebView(uri);
        }

        string videoFileExtension = Path.GetExtension(uri);
        if (ApplicationUtils.IsUnitySupportedVideoFormat(videoFileExtension))
        {
            return LoadWithVideoPlayer(localSongMeta, uri);
        }
        else if (settings.UseFfmpegToPlayMediaFiles)
        {
            return LoadWithFfmpeg(localSongMeta, uri);
        }
        else
        {
            return ObservableUtils.LogErrorThenThrow<SongVideoLoadedEvent>(
                new SongAudioPlayerException($"Unsupported video resource '{uri}'."));
        }
    }

    private IObservable<SongVideoLoadedEvent> LoadWithFfmpeg(SongMeta localSongMeta, string uri)
    {
        Debug.Log($"SongVideoPlayer loading video via ffmpeg: '{uri}'");
        
        VideoSupportProvider = EVideoSupportProvider.Ffmpeg;
        ResetWebViewRenderTexture();
        SetFfmpegRenderTextureToVideoRenderTexture();
        ShowVideoImageVisualElement();
        
        return Observable.Create<SongVideoLoadedEvent>(o =>
        {
            FireLoadedEvent(o, localSongMeta, uri);
            return Disposable.Empty;
        });
    }
    
    private IObservable<SongVideoLoadedEvent> LoadWithWebView(string uri)
    {
        VideoSupportProvider = EVideoSupportProvider.WebView;
        ResetFfmpegRenderTexture();
        SetWebViewRenderTextureToVideoRenderTexture();
        ShowVideoImageVisualElement();
        
        return Observable.Create<SongVideoLoadedEvent>(o =>
        {
            FireLoadedEvent(o, songMeta, uri);
            return Disposable.Empty;
        });
    }
    
    private IObservable<SongVideoLoadedEvent> LoadWithVideoPlayer(SongMeta localSongMeta, string uri)
    {
        VideoSupportProvider = EVideoSupportProvider.UnityVideoPlayer;
        ResetFfmpegRenderTexture();
        ResetWebViewRenderTexture();

        videoPlayer.url = ApplicationUtils.GetVideoPlayerUri(uri);
        if (videoPlayer.url.IsNullOrEmpty())
        {
            // The url is empty if loading the video failed.
            VideoSupportProvider = EVideoSupportProvider.None;
        }
        
        videoPlayer.Pause();
        
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
                        if (string.Equals(localSongMeta.Mp3, localSongMeta.Video, StringComparison.InvariantCultureIgnoreCase))
                        {
                            Debug.Log($"Trying to load video with ffmpeg because Unity's VideoPlayer failed: '{uri}'");
                            LoadWithFfmpeg(localSongMeta, uri)
                                .Subscribe(o.OnNext, o.OnError, o.OnCompleted);
                        }
                        else
                        {
                            Debug.LogError($"Failed to load video with Unity's VideoPlayer and cannot use ffmpeg because the video and audio resource are not equal. Video URI: '{uri}', Video: '{localSongMeta.Video}', Audio URI: '{localSongMeta.Mp3}'");
                        }
                        return;
                    }
                    
                    ShowVideoImageVisualElement();
                    
                    FireLoadedEvent(o, localSongMeta, uri);
                }));
            return Disposable.Empty;
        });
    }

    private void FireLoadedEvent(IObserver<SongVideoLoadedEvent> o, SongMeta songMeta, string uri)
    {
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
        
        if (webViewManager.webViewCamera.targetTexture != videoPlayer.targetTexture)
        {
            webViewManager.webViewCamera.targetTexture = videoPlayer.targetTexture;
        }
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

        if (VideoSupportProvider is EVideoSupportProvider.UnityVideoPlayer)
        {
            videoPlayer.Stop();
            videoPlayer.clip = null;
            videoPlayer.source = VideoSource.VideoClip;
            ClearOutRenderTexture(videoPlayer.targetTexture);
        }
        else if (VideoSupportProvider is EVideoSupportProvider.Ffmpeg)
        {
            ClearOutRenderTexture(videoPlayer.targetTexture);
        }
        else if (VideoSupportProvider is EVideoSupportProvider.WebView)
        {
            ClearOutRenderTexture(videoPlayer.targetTexture);
        }
        VideoSupportProvider = EVideoSupportProvider.None;
    }

    private void SyncVideoWithMusic(bool forceImmediateSync)
    {
        SyncVideoPlayPause(songAudioPlayer.PositionInSongInMillis);
        if (videoPlayer.isPlaying || forceImmediateSync)
        {
            SyncVideoWithMusic(songAudioPlayer.PositionInSongInMillis, songAudioPlayer.DurationOfSongInMillis, forceImmediateSync);
        }
    }

    private void StartVideoPlayback()
    {
        if (!HasLoadedVideo)
        {
            Debug.LogWarning("No video has been loaded. Showing background image instead.");
            ShowBackgroundImage();
            return;
        }

        if (SongMeta.VideoGap > 0)
        {
            // Positive VideoGap, thus skip the start of the video
            PositionInVideoInSeconds = SongMeta.VideoGap;
        }

        if (videoImageVisualElement != null)
        {
            videoImageVisualElement.ShowByDisplay();
            videoImageVisualElement.style.opacity = 1;
        }
    }

    private void SyncVideoPlayPause(double positionInSongInMillis)
    {
        if (!HasLoadedVideo || !videoPlayer.gameObject.activeInHierarchy)
        {
            return;
        }

        bool songAudioPlayerIsPlaying = (songAudioPlayer == null || songAudioPlayer.IsPlaying);

        if ((!songAudioPlayerIsPlaying && videoPlayer.isPlaying)
            || (videoPlayer.length > 0
                && videoPlayer.length <= songAudioPlayer.PositionInSongInSeconds
                && !videoPlayer.isLooping)
            || freezeVideo)
        {
            videoPlayer.Pause();
        }
        else if (songAudioPlayerIsPlaying
                 && !videoPlayer.isPlaying
                 && !IsWaitingForVideoGap(positionInSongInMillis))
        {
            videoPlayer.Play();
        }
    }

    public void SyncVideoWithMusic(double positionInSongInMillis, double durationOfSongInMillis, bool forceImmediateSync)
    {
        if (!HasLoadedVideo || IsWaitingForVideoGap(positionInSongInMillis)
            || (!forceImmediateSync && nextSyncTimeInSeconds > Time.time))
        {
            return;
        }

        // Loop short videos
        double durationOfSongInSeconds = durationOfSongInMillis / 1000;
        videoPlayer.isLooping = videoPlayer.length < durationOfSongInSeconds / 2;
        
        // Both, the smooth sync and immediate sync need some time.
        nextSyncTimeInSeconds = Time.time + 1;

        double targetPositionInVideoInSeconds = SongMeta.VideoGap + positionInSongInMillis / 1000;
        if (videoPlayer.isLooping)
        {
            targetPositionInVideoInSeconds %= videoPlayer.length;
        }

        double timeDifferenceInSeconds = targetPositionInVideoInSeconds - videoPlayer.time;

        if (freezeVideo)
        {
            PlaybackSpeed = 0;
        }
        else
        {
            // A short mismatch in video and song position is smoothed out by adjusting the playback speed of the video.
            // A big mismatch is corrected immediately.
            if (forceImmediateSync || Math.Abs(timeDifferenceInSeconds) > 3)
            {
                // Correct the mismatch immediately.
                PositionInVideoInSeconds = targetPositionInVideoInSeconds;
                PlaybackSpeed = 1f;
            }
            else
            {
                // Smooth out the time difference over a duration of 2 seconds
                float playbackSpeed = 1 + (float)(timeDifferenceInSeconds / 2.0);
                PlaybackSpeed = playbackSpeed;
            }
        }
    }

    // Returns true if still waiting for the start of the video at the given position in the song.
    private bool IsWaitingForVideoGap(double positionInSongInMillis)
    {
        // A negative video gap means this duration has to be waited before playing the video.
        return SongMeta.VideoGap < 0 && positionInSongInMillis < (-SongMeta.VideoGap * 1000);
    }

    public void ShowBackgroundImage()
    {
        if (videoImageVisualElement != null)
        {
            videoImageVisualElement.HideByDisplay();
            videoImageVisualElement.style.opacity = 0;
        }
        if (SongMeta.Background.IsNullOrEmpty())
        {
            ShowCoverImageAsBackground();
            return;
        }

        string backgroundUri = SongMetaUtils.GetBackgroundUri(SongMeta);
        if (!SongMetaUtils.BackgroundResourceExists(songMeta))
        {
            Debug.LogWarning("Showing cover image because background image resource does not exist: " + backgroundUri);
            ShowCoverImageAsBackground();
            return;
        }

        LoadBackgroundImage(backgroundUri);
    }

    private void ShowCoverImageAsBackground()
    {
        string coverUri = SongMetaUtils.GetCoverUri(SongMeta);
        if (coverUri.IsNullOrEmpty())
        {
            return;
        }

        if (!SongMetaUtils.CoverResourceExists(SongMeta))
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
        LoadSongVideoAsObservable(songMeta)
            .CatchIgnore((Exception ex) => Debug.LogException(ex))
            // Subscribe to trigger the observable
            .Subscribe(evt => Debug.Log($"Loaded video: {evt.VideoUri}"));
    }

    private IObservable<SongVideoLoadedEvent> LoadSongVideoAsObservable(SongMeta localSongMeta)
    {
        UnloadVideo();

        // Use the audio URL as video if the WebView can handle it (e.g. a YouTube video).
        string videoUri = SongMetaUtils.GetVideoUriPreferAudioUriIfWebView(songMeta, WebViewUtils.CanHandleWebViewUrl);

        if (videoUri.IsNullOrEmpty())
        {
            return ObservableUtils.LogErrorThenThrow<SongVideoLoadedEvent>(
                new SongVideoPlayerException($"Ignoring empty video resource"));
        }

        if (ignoredVideoFiles.Contains(localSongMeta.Video))
        {
            return ObservableUtils.LogErrorThenThrow<SongVideoLoadedEvent>(
                new SongVideoPlayerException($"Ignoring video resource: '{videoUri}'"));
        }

        if (!SongMetaUtils.ResourceExists(localSongMeta, videoUri))
        {
            return ObservableUtils.LogErrorThenThrow<SongVideoLoadedEvent>(
                new SongVideoPlayerException($"Video resource does not exist: {videoUri}"));
        }

        return LoadVideoAsObservable(localSongMeta, videoUri);
    }

    void OnEnable()
    {
        videoPlayer.errorReceived += OnVideoPlayerErrorReceived;
    }

    void OnDisable()
    {
        videoPlayer.errorReceived -= OnVideoPlayerErrorReceived;
    }

    private void OnDestroy()
    {
        ClearOutRenderTexture(videoPlayer.targetTexture);
    }

    private void OnVideoPlayerErrorReceived(VideoPlayer source, string message)
    {
        Debug.LogError($"SongVideoPlayer received VideoPlayer error: {message}");
        videoPlayerErrorMessages.Add(message);
    }

    // If not cleared, then the RenderTexture will keep its last viewed frame until it is overwritten by a new video.
    // This would cause the last played video to show up for a moment
    // before a new video is loaded and applied to the RenderTexture.
    // Thus, the texture should be cleared before showing a new video.
    private void ClearOutRenderTexture(RenderTexture renderTexture)
    {
        // See https://answers.unity.com/questions/1511295/how-do-i-reset-a-render-texture-to-black-when-i-st.html
        RenderTexture rt = RenderTexture.active;
        RenderTexture.active = renderTexture;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = rt;
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

    public static void AddIgnoredVideoFile(string uri)
    {
        ignoredVideoFiles.Add(uri);
    }
}
