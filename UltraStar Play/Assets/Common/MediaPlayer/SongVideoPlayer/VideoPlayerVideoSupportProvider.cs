using System;
using System.Collections.Generic;
using System.IO;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.Video;

public class VideoPlayerVideoSupportProvider : AbstractVideoSupportProvider
{
    [InjectedInInspector]
    public VideoPlayer videoPlayer;

    private readonly List<string> videoPlayerErrorMessages = new List<string>();

    void OnEnable()
    {
        videoPlayer.errorReceived += OnVideoPlayerErrorReceived;
    }

    void OnDisable()
    {
        videoPlayer.errorReceived -= OnVideoPlayerErrorReceived;
    }

    public override bool IsSupported(string videoUri, bool videoEqualsAudio)
    {
        return !WebViewUtils.CanHandleWebViewUrl(videoUri)
            && settings.VlcToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Always
            && ApplicationUtils.IsUnitySupportedVideoFormat(Path.GetExtension(videoUri));
    }

    public override async Awaitable<VideoLoadedEvent> LoadAsync(string videoUri, double startPositionInMillis)
    {
        videoPlayerErrorMessages.Clear();
        videoPlayer.url = videoUri;
        if (videoPlayer.url.IsNullOrEmpty() && !videoUri.IsNullOrEmpty())
        {
            // The url is empty if loading the video failed.
            throw new SongVideoPlayerException($"Unable to load video '{videoUri}' with Unity's VideoPlayer");
        }

        // Start VideoPlayer to trigger loading
        videoPlayer.Play();
        PositionInMillis = startPositionInMillis;

        // The video is loaded asynchronously. The length property of the VideoPlayer indicates whether it has been loaded.
        await ConditionUtils.WaitForConditionAsync(() => !this
                                                    || videoPlayer.length > 0
                                                    || videoPlayerErrorMessages.Count > 0);
        if (!this)
        {
            throw new DestroyedAlreadyException($"Failed to load video '{videoUri}': {nameof(VideoPlayerVideoSupportProvider)} has been destroyed already.");
        }

        if (videoPlayerErrorMessages.Count > 0)
        {
            Unload();
            throw new VideoSupportProviderException($"Failed to load video: '{videoUri}'");
        }

        return new VideoLoadedEvent(videoUri);
    }

    public override void Unload()
    {
        RenderTextureUtils.Clear(videoPlayer.targetTexture);
        videoPlayerErrorMessages.Clear();
    }

    public override void Play()
    {
        videoPlayer.Play();
    }

    public override void Pause()
    {
        videoPlayer.Pause();
    }

    public override void Stop()
    {
        videoPlayer.Stop();
        videoPlayer.clip = null;
        videoPlayer.source = VideoSource.VideoClip;
    }

    public override void SetTargetTexture(RenderTexture renderTexture)
    {
        // Ignore
    }

    public override void SetBackgroundScaleMode(ESongBackgroundScaleMode mode)
    {
        switch (mode)
        {
            case ESongBackgroundScaleMode.FitInside:
                videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
                return;
            case ESongBackgroundScaleMode.FitOutside:
                videoPlayer.aspectRatio = VideoAspectRatio.FitOutside;
                return;
        }
    }

    public override bool IsPlaying
    {
        get => videoPlayer.isPlaying;
        set
        {
            if (value)
            {
                Play();
            }
            else
            {
                Pause();
            }
        }
    }

    public override bool IsLooping
    {
        get => videoPlayer.isLooping;
        set => videoPlayer.isLooping = value;
    }

    public override double PlaybackSpeed
    {
        get => videoPlayer.playbackSpeed;
        set => videoPlayer.playbackSpeed = (float)value;
    }

    public override double PositionInMillis
    {
        get => videoPlayer.time * 1000;
        set => videoPlayer.time = value / 1000.0;
    }

    public override double DurationInMillis => videoPlayer.length * 1000.0;

    private void OnVideoPlayerErrorReceived(VideoPlayer source, string message)
    {
        Debug.LogError($"Unity VideoPlayer has error: {message}");
        videoPlayerErrorMessages.Add(message);
    }
}
