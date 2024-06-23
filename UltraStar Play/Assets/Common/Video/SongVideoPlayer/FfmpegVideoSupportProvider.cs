using System;
using System.IO;
using UniInject;
using UniRx;
using UnityEngine;

public class FfmpegVideoSupportProvider : AbstractVideoSupportProvider
{
    [Inject]
    private SongAudioPlayer songAudioPlayer;

    public override bool IsSupported(string videoUri, bool videoEqualsAudio)
    {
        return !WebRequestUtils.IsHttpOrHttpsUri(videoUri)
               && settings.FfmpegToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Never
               && ApplicationUtils.IsFfmpegSupportedAudioFormat(Path.GetExtension(videoUri));
    }

    public override IObservable<VideoLoadedEvent> LoadAsObservable(string videoUri)
    {
        // Loading is done by SongAudioPlayer
        return Observable.Return<VideoLoadedEvent>(new VideoLoadedEvent(videoUri));
    }

    public override void Unload()
    {
        ResetFfmpegRenderTexture();
    }

    public override void Play()
    {
        // Handled by SongAudioPlayer
    }

    public override void Pause()
    {
        // Handled by SongAudioPlayer
    }

    public override void Stop()
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
        get => false;
        set { /* Not supported */ }
    }

    public override double PlaybackSpeed
    {
        get => 1;
        set { /* Not supported */ }
    }

    public override double PositionInMillis
    {
        get => songAudioPlayer.PositionInMillis;
        set => songAudioPlayer.PositionInMillis = value;
    }

    public override double DurationInMillis => songAudioPlayer.DurationInMillis;

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
