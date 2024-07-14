using System;
using LibVLCSharp;
using UniInject;
using UniRx;

public class VlcVideoSupportProvider : AbstractVlcVideoSupportProvider
{
    [Inject]
    private VlcManager vlcManager;

    private long lastVlcMediaPlayerTimeInMillisWhenPlaying;
    private bool isPaused;

    protected override void Update()
    {
        base.Update();

        if (!isPaused && IsPlaying)
        {
            lastVlcMediaPlayerTimeInMillisWhenPlaying = mediaPlayer.Time;
        }

        UpdateVlcMediaPlayerPause();
    }

    private void UpdateVlcMediaPlayerPause()
    {
        // TODO: Workaround for unreliable VLC MediaPlayer pause state ( https://code.videolan.org/videolan/vlc/-/issues/28353 )
        if (isPaused && mediaPlayer != null && mediaPlayer.IsPlaying)
        {
            Log.Verbose(() => "Should be paused but VLC MediaPlayer is playing anyway. Set VLC MediaPlayer to pause again.");
            mediaPlayer.SetPause(true);
            PositionInMillis = lastVlcMediaPlayerTimeInMillisWhenPlaying;
        }
    }

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

        // Play to trigger loading. PlayAsync to not block the main thread and avoid stutter.
        mediaPlayer.PlayAsync();

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
        isPaused = false;
        mediaPlayer?.PlayAsync();
    }

    public override void Pause()
    {
        isPaused = true;
        mediaPlayer?.Pause();
    }

    public override void Stop()
    {
        isPaused = false;
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
        get
        {
            // VLC MediaPlayer continues time even if not playing. Workaround: return old time if not playing.
            return !isPaused && mediaPlayer.IsPlaying
                ? mediaPlayer.Time
                : lastVlcMediaPlayerTimeInMillisWhenPlaying;
        }
        set
        {
            lastVlcMediaPlayerTimeInMillisWhenPlaying = (long)value;
            mediaPlayer?.SetTime((long)value);
        }
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
