using System;
using LibVLCSharp;
using UniRx;

public class VlcVideoSupportProvider : AbstractVlcVideoSupportProvider
{
    private VlcManager vlcManager;
    private MediaPlayer vlcMediaPlayer;

    protected override void OnDestroy()
    {
        base.OnDestroy();
        DestroyVlcMediaPlayer();
    }

    public override bool IsSupported(string videoUri, SongMeta songMeta)
    {
        return base.IsSupported(videoUri, songMeta)
               && songMeta.Audio != songMeta.Video;
    }

    public override IObservable<VideoLoadedEvent> LoadVideoAsObservable(string videoUri)
    {
        // Instantiate new vlc player
        if (vlcMediaPlayer == null)
        {
            vlcMediaPlayer = vlcManager.CreateMediaPlayer();
            vlcManager.DisableMediaPlayerAudioOutput(vlcMediaPlayer);
        }
        else
        {
            vlcMediaPlayer.Stop();
        }

        if (vlcMediaPlayer.Media != null)
        {
            vlcMediaPlayer.Media.Dispose();
        }

        vlcMediaPlayer.Media = new Media(new Uri(videoUri));
        vlcMediaPlayer.PlayAsync();

        // The video is loaded asynchronously.
        // The duration property indicates whether it has been loaded.
        return Observable.Create<VideoLoadedEvent>(o =>
        {
            StartCoroutine(CoroutineUtils.ExecuteWhenConditionIsTrue(
                () => vlcMediaPlayer.Media != null && vlcMediaPlayer.Media.Duration > 0,
                () => o.OnNext(new VideoLoadedEvent(videoUri))));
            return Disposable.Empty;
        });
    }

    public override void UnloadVideo()
    {
        DestroyVlcMediaPlayer();
    }

    public override void PlayVideo()
    {
        vlcMediaPlayer?.Play();
    }

    public override void PauseVideo()
    {
        vlcMediaPlayer?.Pause();
    }

    public override void StopVideo()
    {
        vlcMediaPlayer?.Stop();
    }

    public override bool IsPlaying
    {
        get => vlcMediaPlayer?.IsPlaying ?? false;
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
        set
        {
            // TODO: Using MediaPlayer.SetRate makes the video stutter
            // vlcMediaPlayer?.SetRate(playbackSpeed);
        }
    }

    public override double PositionInVideoInMillis
    {
        get => vlcMediaPlayer?.Time ?? 0;
        set => vlcMediaPlayer?.SetTime((long)value);
    }

    public override double DurationInMillis => vlcMediaPlayer.Length;

    private void DestroyVlcMediaPlayer()
    {
        if (vlcMediaPlayer == null)
        {
            return;
        }

        VlcManager.DestroyMediaPlayer(vlcMediaPlayer);
        vlcMediaPlayer = null;
    }
}
