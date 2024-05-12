using System;
using UniInject;
using UniRx;

public class SongAudioPlayerVlcVideoSupportProvider : AbstractVlcVideoSupportProvider
{
    [Inject]
    private SongAudioPlayer songAudioPlayer;

    public override bool IsSupported(string videoUri, bool videoEqualsAudio)
    {
        return base.IsSupported(videoUri, videoEqualsAudio)
            && videoEqualsAudio
            && songAudioPlayer.VlcMediaPlayer != null;
    }

    public override IObservable<VideoLoadedEvent> LoadAsObservable(string videoUri)
    {
        return Observable.Create<VideoLoadedEvent>(o =>
        {
            StartCoroutine(CoroutineUtils.ExecuteWhenConditionIsTrue(
                () => songAudioPlayer.VlcMediaPlayer != null
                      && songAudioPlayer.VlcMediaPlayer.Media != null
                      && songAudioPlayer.VlcMediaPlayer.Media.Duration > 0,
                () =>
                {
                    mediaPlayer = songAudioPlayer.VlcMediaPlayer;
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
        get => songAudioPlayer.PositionInSongInMillis;
        set => songAudioPlayer.PositionInSongInMillis = value;
    }

    public override double DurationInMillis => songAudioPlayer.DurationOfSongInMillis;
}
