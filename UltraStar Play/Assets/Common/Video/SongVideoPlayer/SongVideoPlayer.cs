using System;
using UnityEngine;
using UniInject;
using UnityEngine.Video;
using System.IO;
using UnityEngine.UI;
using UniRx;

public class SongVideoPlayer : MonoBehaviour
{
    [InjectedInInspector]
    public VideoPlayer videoPlayer;

    [InjectedInInspector]
    public Image backgroundImage;

    [InjectedInInspector]
    public Image videoImage;

    public bool forceSyncOnForwardJumpInTheSong;

    // Optional SongAudioPlayer.
    // If set, then the video playback is synchronized with the position in the SongAudioPlayer.
    public SongAudioPlayer SongAudioPlayer { get; set; }

    private SongMeta SongMeta { get; set; }

    public bool HasLoadedVideo { get; private set; }
    public bool HasLoadedBackgroundImage { get; private set; }
    private string videoPath;

    private float nextSyncTimeInSeconds;

    private IDisposable jumpBackInSongEventStreamDisposable;
    private IDisposable jumpForwardInSongEventStreamDisposable;

    public string videoPlayerErrorMessage;

    public void Init(SongMeta songMeta, SongAudioPlayer songAudioPlayer)
    {
        this.SongMeta = songMeta;
        this.SongAudioPlayer = songAudioPlayer;
        HasLoadedBackgroundImage = false;
        InitEventSubscriber();
        InitVideo(songMeta);
    }

    private void InitEventSubscriber()
    {
        // Jump backward in song
        if (jumpBackInSongEventStreamDisposable != null)
        {
            jumpBackInSongEventStreamDisposable.Dispose();
        }
        jumpBackInSongEventStreamDisposable = SongAudioPlayer.JumpBackInSongEventStream
            .Subscribe(_ => SyncVideoWithMusic(true));

        // Jump forward in song
        if (jumpForwardInSongEventStreamDisposable != null)
        {
            jumpForwardInSongEventStreamDisposable.Dispose();
        }
        if (forceSyncOnForwardJumpInTheSong)
        {
            jumpForwardInSongEventStreamDisposable = SongAudioPlayer.JumpForwardInSongEventStream
                .Subscribe(_ => SyncVideoWithMusic(true));
        }
    }

    void Update()
    {
        if (!videoPlayerErrorMessage.IsNullOrEmpty())
        {
            UiManager.Instance.CreateNotification(videoPlayerErrorMessage, Colors.red);
            videoPlayerErrorMessage = "";
            UnloadVideo();
            // Do not attempt to load the video again
            SongMeta.Video = "";
        }

        if (!HasLoadedVideo)
        {
            return;
        }

        if (SongAudioPlayer != null)
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

    public void LoadVideo(string videoPath)
    {
        if (!File.Exists(videoPath))
        {
            Debug.LogWarning("Video does not exist: " + videoPath);
            return;
        }

        videoPlayer.url = "file://" + videoPath;
        // The url is empty if loading the video failed.
        HasLoadedVideo = !string.IsNullOrEmpty(videoPlayer.url);
        // For now, only load the video. Starting it is done from the outside.
        if (HasLoadedVideo)
        {
            videoImage.gameObject.SetActive(true);
            videoPlayer.Pause();
        }
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
        SyncVideoPlayPause(SongAudioPlayer.PositionInSongInMillis);
        if (videoPlayer.isPlaying || forceImmediateSync)
        {
            SyncVideoWithMusic(SongAudioPlayer.PositionInSongInMillis, forceImmediateSync);
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

        bool songAudioPlayerIsPlaying = (SongAudioPlayer == null || SongAudioPlayer.IsPlaying);

        if ((!songAudioPlayerIsPlaying && videoPlayer.isPlaying)
            || (videoPlayer.length > 0
                && (videoPlayer.length * 1000) <= SongAudioPlayer.PositionInSongInMillis))
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
        videoImage.gameObject.SetActive(false);
        if (string.IsNullOrEmpty(SongMeta.Background))
        {
            ShowCoverImageAsBackground();
            return;
        }

        string backgroundImagePath = SongMeta.Directory + Path.DirectorySeparatorChar + SongMeta.Background;
        if (File.Exists(backgroundImagePath))
        {
            LoadBackgroundImage(backgroundImagePath);
        }
        else
        {
            Debug.LogWarning("Background image '" + backgroundImagePath + "'does not exist. Showing cover instead.");
            ShowCoverImageAsBackground();
        }
    }

    private void ShowCoverImageAsBackground()
    {
        if (string.IsNullOrEmpty(SongMeta.Cover))
        {
            return;
        }

        string coverImagePath = SongMeta.Directory + Path.DirectorySeparatorChar + SongMeta.Cover;
        if (File.Exists(coverImagePath))
        {
            LoadBackgroundImage(coverImagePath);
        }
        else
        {
            Debug.LogWarning("Cover image '" + coverImagePath + "'does not exist.");
        }
    }

    private void LoadBackgroundImage(string imagePath)
    {
        Sprite coverImageSprite = ImageManager.LoadSprite(imagePath);
        backgroundImage.sprite = coverImageSprite;
        HasLoadedBackgroundImage = true;
    }

    private void InitVideo(SongMeta songMeta)
    {
        UnloadVideo();

        videoPath = "";
        if (!string.IsNullOrEmpty(songMeta.Video))
        {
            videoPath = songMeta.Directory + Path.DirectorySeparatorChar + songMeta.Video;
            LoadVideo(videoPath);
        }
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
