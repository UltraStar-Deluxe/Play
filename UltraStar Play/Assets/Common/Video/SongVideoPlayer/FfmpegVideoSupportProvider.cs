using System;
using UniInject;
using UniRx;
using UnityEngine;

public class FfmpegVideoSupportProvider : AbstractVideoSupportProvider
{
    public override EVideoSupportProvider VideoSupportProvider => EVideoSupportProvider.Ffmpeg;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    protected void OnDestroy()
    {
        ResetFfmpegRenderTexture();
    }

    public override IObservable<VideoLoadedEvent> LoadVideoAsObservable(string videoUri)
    {
        // Loading is done by SongAudioPlayer
        return Observable.Return<VideoLoadedEvent>(new VideoLoadedEvent(videoUri));
    }

    public override void UnloadVideo()
    {
        ResetFfmpegRenderTexture();
    }

    public override void PlayVideo()
    {
        // Handled by SongAudioPlayer
    }

    public override void PauseVideo()
    {
        // Handled by SongAudioPlayer
    }

    public override void StopVideo()
    {
        // Handled by SongAudioPlayer
    }

    public override bool IsPlaying
    {
        get => songAudioPlayer.IsPlaying;
        set
        {
            if (value)
            {
                PlayVideo();
            }
            else
            {
                PauseVideo();
            }
        }
    }

    public override bool IsLooping
    {
        get => false;
        set { /* Not supported */ }
    }

    public override float PlaybackSpeed
    {
        get => 1;
        set { /* Not supported */ }
    }

    public override double PositionInVideoInMillis
    {
        get => songAudioPlayer.PositionInSongInMillis;
        set => songAudioPlayer.PositionInSongInMillis = value;
    }

    public override double DurationInMillis => songAudioPlayer.DurationOfSongInMillis;

    public override void SetTargetTexture(RenderTexture renderTexture)
    {
        if (renderTexture != null)
        {
            SetFfmpegRenderTextureToVideoRenderTexture(renderTexture);
        }
        else
        {
            ResetFfmpegRenderTexture();
        }
    }

    private void SetFfmpegRenderTextureToVideoRenderTexture(RenderTexture renderTexture)
    {
        songAudioPlayer.FfmpegRenderTexture = renderTexture;
    }

    private void ResetFfmpegRenderTexture()
    {
        songAudioPlayer.FfmpegRenderTexture = null;
    }
}
