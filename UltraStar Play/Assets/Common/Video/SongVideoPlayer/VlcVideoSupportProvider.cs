using System;
using LibVLCSharp;
using UniInject;
using UniRx;

public class VlcVideoSupportProvider : AbstractVlcVideoSupportProvider
{
    [Inject]
    private VlcManager vlcManager;

    public override bool IsSupported(string videoUri, bool videoEqualsAudio)
    {
        return base.IsSupported(videoUri, videoEqualsAudio)
               // The SongAudioPlayer's mediaPlayer should be used when video and audio are equal
               && !videoEqualsAudio;
    }

    public override IObservable<VideoLoadedEvent> LoadAsObservable(string videoUri)
    {
        // Instantiate new vlc player
        if (mediaPlayer == null)
        {
            mediaPlayer = vlcManager.CreateMediaPlayer();
            vlcManager.DisableMediaPlayerAudioOutput(mediaPlayer);
        }
        else
        {
            mediaPlayer.Stop();
        }

        if (mediaPlayer.Media != null)
        {
            mediaPlayer.Media.Dispose();
        }

        mediaPlayer.Media = new Media(new Uri(videoUri));
        mediaPlayer.Play();

        // The video is loaded asynchronously.
        // The duration property indicates whether it has been loaded.
        return Observable.Create<VideoLoadedEvent>(o =>
        {
            StartCoroutine(CoroutineUtils.ExecuteWhenConditionIsTrue(
                () => mediaPlayer.Media != null && mediaPlayer.Media.Duration > 0,
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
        mediaPlayer?.Play();
    }

    public override void Pause()
    {
        mediaPlayer?.Pause();
    }

    public override void Stop()
    {
        mediaPlayer?.Stop();
    }

    public override bool IsPlaying
    {
        get => mediaPlayer?.IsPlaying ?? false;
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
            // mediaPlayer?.SetRate(playbackSpeed);
        }
    }

    public override double PositionInMillis
    {
        get => mediaPlayer?.Time ?? 0;
        set => mediaPlayer?.SetTime((long)value);
    }

    public override double DurationInMillis => mediaPlayer.Length;

    private void DestroyVlcMediaPlayer()
    {
        if (mediaPlayer == null)
        {
            return;
        }

        VlcManager.DestroyMediaPlayer(mediaPlayer);
        mediaPlayer = null;
    }
}
