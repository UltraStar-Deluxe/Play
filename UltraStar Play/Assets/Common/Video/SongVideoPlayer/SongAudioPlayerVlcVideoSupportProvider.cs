using System;
using LibVLCSharp;
using UniInject;
using UniRx;

public class SongAudioPlayerVlcVideoSupportProvider : AbstractVlcVideoSupportProvider
{
    [Inject]
    private SongAudioPlayer songAudioPlayer;

    private MediaPlayer SongAudioPlayerVlcMediaPlayer
    {
        get
        {
            return songAudioPlayer.CurrentAudioSupportProvider is VlcAudioSupportProvider vlcAudioSupportProvider
                ? vlcAudioSupportProvider.VlcMediaPlayer
                : null;
        }
    }

    public override bool IsSupported(string videoUri, bool videoEqualsAudio)
    {
        return base.IsSupported(videoUri, videoEqualsAudio)
            && videoEqualsAudio
            && SongAudioPlayerVlcMediaPlayer != null;
    }

    public override IObservable<VideoLoadedEvent> LoadAsObservable(string videoUri)
    {
        return Observable.Create<VideoLoadedEvent>(o =>
        {
            StartCoroutine(CoroutineUtils.ExecuteWhenConditionIsTrue(
                () => SongAudioPlayerVlcMediaPlayer != null
                      && SongAudioPlayerVlcMediaPlayer.Media != null
                      && SongAudioPlayerVlcMediaPlayer.Media.Duration > 0,
                () =>
                {
                    mediaPlayer = SongAudioPlayerVlcMediaPlayer;
                    o.OnNext(new VideoLoadedEvent(videoUri));
                }));
            return Disposable.Empty;
        });
    }

    public override void Unload()
    {
        base.Unload();
        mediaPlayer = null;

        // Rest is handled by SongAudioPlayer
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
                songAudioPlayer.PlayAudio();
            }
            else
            {
                songAudioPlayer.PauseAudio();
            }
        }
    }

    public override bool IsLooping
    {
        get => false;
        set { /* Not available */ }
    }

    public override double PlaybackSpeed
    {
        get => 1;
        set { /* Not available */ }
    }

    public override double PositionInMillis
    {
        get => songAudioPlayer.PositionInMillis;
        set => songAudioPlayer.PositionInMillis = value;
    }

    public override double DurationInMillis => songAudioPlayer.DurationInMillis;
}
