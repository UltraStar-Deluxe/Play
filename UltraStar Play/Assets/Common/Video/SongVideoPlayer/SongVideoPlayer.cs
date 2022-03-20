using System;
using System.Collections.Generic;
using UnityEngine;
using UniInject;
using UnityEngine.Video;
using UniRx;
using UnityEngine.UIElements;

public class SongVideoPlayer : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    private static readonly HashSet<string> ignoredVideoFiles = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        ignoredVideoFiles.Clear();
    }

    [InjectedInInspector]
    public VideoPlayer videoPlayer;

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
            }
        }
    }

    [Inject(UxmlName = R.UxmlNames.songImage, Optional = true)]
    private VisualElement backgroundImageVisualElement;

    public bool forceSyncOnForwardJumpInTheSong;

    // SongAudioPlayer to synchronize the playback position with.
    [Inject]
    private SongAudioPlayer songAudioPlayer;

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
            InitVideo(songMeta);
        }
    }

    public bool HasLoadedVideo { get; private set; }
    public bool HasLoadedBackgroundImage { get; private set; }

    private float nextSyncTimeInSeconds;

    private IDisposable jumpBackInSongEventStreamDisposable;
    private IDisposable jumpForwardInSongEventStreamDisposable;

    public string videoPlayerErrorMessage;

    public void OnInjectionFinished()
    {
        HasLoadedBackgroundImage = false;
        InitEventSubscriber();
        InitVideo(SongMeta);
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
        if (!videoPlayerErrorMessage.IsNullOrEmpty())
        {
            Debug.LogError(videoPlayerErrorMessage);
            UiManager.Instance.CreateNotificationVisualElement(videoPlayerErrorMessage, "error");
            videoPlayerErrorMessage = "";
            UnloadVideo();
            // Do not attempt to load the video again
            if (songMeta != null)
            {
                ignoredVideoFiles.Add(songMeta.Video);
            }
        }

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

    private void LoadVideo(string uri)
    {
        if (!WebRequestUtils.ResourceExists(uri))
        {
            Debug.LogWarning("Video file resource does not exist: " + uri);
            return;
        }

        videoPlayer.url = uri;
        // The url is empty if loading the video failed.
        HasLoadedVideo = !string.IsNullOrEmpty(videoPlayer.url);
        // For now, only load the video. Starting it is done from the outside.
        if (!HasLoadedVideo)
        {
            return;
        }

        if (videoImageVisualElement != null)
        {
            videoImageVisualElement.ShowByDisplay();
        }
        videoPlayer.Pause();
    }

    private void UnloadVideo()
    {
        if (!HasLoadedVideo)
        {
            return;
        }
        HasLoadedVideo = false;
        videoPlayer.Stop();
        videoPlayer.clip = null;
        videoPlayer.source = VideoSource.VideoClip;
        ClearOutRenderTexture(videoPlayer.targetTexture);
    }

    private void SyncVideoWithMusic(bool forceImmediateSync)
    {
        SyncVideoPlayPause(songAudioPlayer.PositionInSongInMillis);
        if (videoPlayer.isPlaying || forceImmediateSync)
        {
            SyncVideoWithMusic(songAudioPlayer.PositionInSongInMillis, forceImmediateSync);
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
            videoPlayer.time = SongMeta.VideoGap;
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
                && (videoPlayer.length * 1000) <= songAudioPlayer.PositionInSongInMillis))
        {
            videoPlayer.Pause();
        }
        else if (songAudioPlayerIsPlaying && !videoPlayer.isPlaying && !IsWaitingForVideoGap(positionInSongInMillis))
        {
            videoPlayer.Play();
        }
    }

    public void SyncVideoWithMusic(double positionInSongInMillis, bool forceImmediateSync)
    {
        if (!HasLoadedVideo || IsWaitingForVideoGap(positionInSongInMillis)
            || (!forceImmediateSync && nextSyncTimeInSeconds > Time.time))
        {
            return;
        }

        // Both, the smooth sync and immediate sync need some time.
        nextSyncTimeInSeconds = Time.time + 0.5f;

        double targetPositionInVideoInSeconds = SongMeta.VideoGap + positionInSongInMillis / 1000;
        double timeDifferenceInSeconds = targetPositionInVideoInSeconds - videoPlayer.time;

        // A short mismatch in video and song position is smoothed out by adjusting the playback speed of the video.
        // A big mismatch is corrected immediately.
        if (forceImmediateSync || Math.Abs(timeDifferenceInSeconds) > 2)
        {
            // Correct the mismatch immediately.
            videoPlayer.time = targetPositionInVideoInSeconds;
            videoPlayer.playbackSpeed = 1f;
        }
        else
        {
            // Smooth out the time difference over a duration of 2 seconds
            float playbackSpeed = 1 + (float)(timeDifferenceInSeconds / 2.0);
            videoPlayer.playbackSpeed = playbackSpeed;
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
        }
        if (string.IsNullOrEmpty(SongMeta.Background))
        {
            ShowCoverImageAsBackground();
            return;
        }

        string backgroundUri = SongMetaUtils.GetBackgroundUri(SongMeta);
        if (!WebRequestUtils.ResourceExists(backgroundUri))
        {
            Debug.LogWarning("Showing cover image because background image resource does not exist: " + backgroundUri);
            ShowCoverImageAsBackground();
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

        if (!WebRequestUtils.ResourceExists(coverUri))
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

    private void InitVideo(SongMeta initSongMeta)
    {
        UnloadVideo();

        if (initSongMeta == null
            || initSongMeta.Video.IsNullOrEmpty()
            || ignoredVideoFiles.Contains(initSongMeta.Video))
        {
            return;
        }

        LoadVideo(SongMetaUtils.GetVideoUri(initSongMeta));
    }

    void OnEnable()
    {
        videoPlayer.errorReceived += OnVideoPlayerErrorReceived;
    }

    void OnDisable()
    {
        videoPlayer.errorReceived -= OnVideoPlayerErrorReceived;
        if (HasLoadedVideo)
        {
            ClearOutRenderTexture(videoPlayer.targetTexture);
        }
    }

    private void OnVideoPlayerErrorReceived(VideoPlayer source, string message)
    {
        videoPlayerErrorMessage = message;
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
}
