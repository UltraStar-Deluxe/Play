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

    private SongMeta SongMeta { get; set; }

    private bool hasLoadedVideo;
    private string videoPath;

    private float nextSmoothSyncTimeInSeconds;

    public void Init(SongMeta songMeta)
    {
        this.SongMeta = songMeta;
        InitVideo(songMeta);
    }

    public void PlayVideo()
    {
        if (!hasLoadedVideo)
        {
            return;
        }
        videoPlayer.Play();
    }

    public void PauseVideo()
    {
        if (!hasLoadedVideo)
        {
            return;
        }
        videoPlayer.Pause();
    }

    public void SetPositionInSongInMillis(double positionInSongInMillis)
    {
        if (!hasLoadedVideo)
        {
            return;
        }

        UpdateVideoStart(positionInSongInMillis);
        if (videoPlayer.isPlaying && nextSmoothSyncTimeInSeconds <= Time.time)
        {
            nextSmoothSyncTimeInSeconds = Time.time + 0.5f;
            SyncVideoWithMusicSmoothly(positionInSongInMillis);
        }
    }

    public void StartVideoOrShowBackgroundImage()
    {
        if (hasLoadedVideo)
        {
            StartVideoPlayback(videoPath);
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

    private void StartVideoPlayback(string videoPath)
    {
        if (string.IsNullOrEmpty(videoPlayer.url))
        {
            Debug.LogWarning("Setting VideoPlayer URL failed. Showing background image instead.");
            ShowBackgroundImage();
        }
        else
        {
            if (SongMeta.VideoGap > 0)
            {
                // Positive VideoGap, thus skip the start of the video
                videoPlayer.time = SongMeta.VideoGap;
                videoPlayer.Play();
            }
            else if (SongMeta.VideoGap < 0)
            {
                // Negative VideoGap, thus wait a little before starting the video
                videoPlayer.Pause();
            }
            else
            {
                // No VideoGap, thus start the video immediately
                videoPlayer.Play();
            }
        }
    }

    private void UpdateVideoStart(double positionInSongInMillis)
    {
        if (!hasLoadedVideo)
        {
            return;
        }

        // Negative VideoGap: Start video after a pause in seconds.
        if (SongMeta.VideoGap < 0
            && videoPlayer.gameObject.activeInHierarchy
            && videoPlayer.isPaused
            && (positionInSongInMillis >= (-SongMeta.VideoGap * 1000)))
        {
            videoPlayer.Play();
        }
    }

    public void SyncVideoWithMusicImmediately(double positionInSongInMillis)
    {
        if (!hasLoadedVideo)
        {
            return;
        }

        if (SongMeta.VideoGap < 0 && positionInSongInMillis < (-SongMeta.VideoGap * 1000))
        {
            // Still waiting for the start of the video
            return;
        }

        double targetPositionInVideoInSeconds = SongMeta.VideoGap + positionInSongInMillis / 1000;
        videoPlayer.time = targetPositionInVideoInSeconds;
        videoPlayer.playbackSpeed = 1f;
        nextSmoothSyncTimeInSeconds = Time.time + 0.5f;
    }

    private void SyncVideoWithMusicSmoothly(double positionInSongInMillis)
    {
        if (!hasLoadedVideo)
        {
            return;
        }

        if (SongMeta.VideoGap < 0 && positionInSongInMillis < (-SongMeta.VideoGap * 1000))
        {
            // Still waiting for the start of the video
            return;
        }

        double positionInVideoInSeconds = SongMeta.VideoGap + positionInSongInMillis / 1000;
        double timeDifferenceInSeconds = positionInVideoInSeconds - videoPlayer.time;
        // Smooth out the time difference over a duration of 2 seconds
        float playbackSpeed = 1 + (float)(timeDifferenceInSeconds / 2.0);
        videoPlayer.playbackSpeed = playbackSpeed;
    }

    public void ShowBackgroundImage()
    {
        videoImageAndPlayerContainer.gameObject.SetActive(false);
        if (string.IsNullOrEmpty(SongMeta.Background))
        {
            ShowCoverImageAsBackground();
        }
        else
        {
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