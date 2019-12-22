using System;
using UnityEngine;
using UniInject;
using UnityEngine.Video;
using System.IO;
using UnityEngine.UI;

public class SongVideoPlayer : MonoBehaviour
{
    [InjectedInInspector]
    public VideoPlayer videoPlayer;

    [InjectedInInspector]
    public GameObject videoImageAndPlayerContainer;

    [InjectedInInspector]
    public Image backgroundImage;

    // Optional SongAudioPlayer.
    // If set, then the video playback is synchronized with the position in the SongAudioPlayer.
    public SongAudioPlayer SongAudioPlayer { get; set; }

    private SongMeta SongMeta { get; set; }

    private bool hasLoadedVideo;
    private string videoPath;

    private float nextSyncTimeInSeconds;

    public void Init(SongMeta songMeta, SongAudioPlayer songAudioPlayer)
    {
        this.SongMeta = songMeta;
        this.SongAudioPlayer = songAudioPlayer;
        InitVideo(songMeta);
    }

    void Update()
    {
        if (SongAudioPlayer != null)
        {
            SynchVideoWithMusic(SongAudioPlayer);
        }
        else
        {
            Debug.Log("no audio player");
        }
    }

    public void StartVideoOrShowBackgroundImage()
    {
        if (hasLoadedVideo)
        {
            StartVideoPlayback();
        }
        else
        {
            Debug.LogWarning("Video file '" + videoPath + "' does not exist. Showing background image instead.");
            ShowBackgroundImage();
        }
    }

    public void LoadVideo(string videoPath)
    {
        videoPlayer.url = "file://" + videoPath;
        // The url is empty if loading the video failed.
        hasLoadedVideo = !string.IsNullOrEmpty(videoPlayer.url);
        // For now, only load the video. Starting it is done from the outside.
        if (hasLoadedVideo)
        {
            videoPlayer.Pause();
        }
    }

    private void SynchVideoWithMusic(SongAudioPlayer songAudioPlayer)
    {
        SyncVideoPlayPause(songAudioPlayer.PositionInSongInMillis);
        if (videoPlayer.isPlaying)
        {
            SyncVideoWithMusic(songAudioPlayer.PositionInSongInMillis);
        }
    }

    private void StartVideoPlayback()
    {
        if (string.IsNullOrEmpty(videoPlayer.url))
        {
            Debug.LogWarning("Setting VideoPlayer URL failed. Showing background image instead.");
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
        if (!hasLoadedVideo || !videoPlayer.gameObject.activeInHierarchy)
        {
            return;
        }

        bool songAudioPlayerIsPlaying = (SongAudioPlayer == null || SongAudioPlayer.IsPlaying);

        if (!songAudioPlayerIsPlaying && videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
        }
        else if (songAudioPlayerIsPlaying && !videoPlayer.isPlaying && !IsWaitingForVideoGap(positionInSongInMillis))
        {
            videoPlayer.Play();
        }
    }

    private void SyncVideoWithMusic(double positionInSongInMillis)
    {
        if (!hasLoadedVideo || IsWaitingForVideoGap(positionInSongInMillis) || nextSyncTimeInSeconds > Time.time)
        {
            return;
        }

        // Both, the smooth sync and immediate sync need some time.
        nextSyncTimeInSeconds = Time.time + 0.5f;

        double targetPositionInVideoInSeconds = SongMeta.VideoGap + positionInSongInMillis / 1000;
        double timeDifferenceInSeconds = targetPositionInVideoInSeconds - videoPlayer.time;

        // A short mismatch in video and song position is smoothed out by adjusting the playback speed of the video.
        // A big mismatch is corrected immediately.
        if (Math.Abs(timeDifferenceInSeconds) > 2)
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
        videoImageAndPlayerContainer.gameObject.SetActive(false);
        if (string.IsNullOrEmpty(SongMeta.Background))
        {
            ShowCoverImageAsBackground();
            return;
        }

        string backgroundImagePath = SongMeta.Directory + Path.DirectorySeparatorChar + SongMeta.Background;
        if (File.Exists(backgroundImagePath))
        {
            Sprite backgroundImageSprite = ImageManager.LoadSprite(backgroundImagePath);
            backgroundImage.sprite = backgroundImageSprite;
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
            Sprite coverImageSprite = ImageManager.LoadSprite(coverImagePath);
            backgroundImage.sprite = coverImageSprite;
        }
        else
        {
            Debug.LogWarning("Cover image '" + coverImagePath + "'does not exist.");
        }
    }

    private void InitVideo(SongMeta songMeta)
    {
        hasLoadedVideo = false;
        videoPath = "";
        if (!string.IsNullOrEmpty(songMeta.Video))
        {
            videoPath = songMeta.Directory + Path.DirectorySeparatorChar + songMeta.Video;
            if (File.Exists(videoPath))
            {
                LoadVideo(videoPath);
            }
        }
    }
}