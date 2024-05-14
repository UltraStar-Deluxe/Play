using System;
using LibVLCSharp;
using UniInject;
using UniRx;

public class VlcVideoSupportProvider : AbstractVlcVideoSupportProvider
{
    [Inject]
    private VlcManager vlcManager;

    private MediaPlayer vlcMediaPlayer;

    public override bool IsSupported(string videoUri, bool videoEqualsAudio)
    {
        return base.IsSupported(videoUri, videoEqualsAudio)
               // The SongAudioPlayer's vlcMediaPlayer should be used when video and audio are equal
               && !videoEqualsAudio;
    }

    public override IObservable<VideoLoadedEvent> LoadAsObservable(string videoUri)
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

    public override void Unload()
    {
        base.Unload();
        DestroyVlcMediaPlayer();
    }

    public override void Play()
    {
        vlcMediaPlayer?.Play();
    }

    public override void Pause()
    {
        vlcMediaPlayer?.Pause();
    }

    public override void Stop()
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
        set
        {
            // TODO: Using MediaPlayer.SetRate makes the video stutter
            // vlcMediaPlayer?.SetRate(playbackSpeed);
        }
    }

    public override double PositionInMillis
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
