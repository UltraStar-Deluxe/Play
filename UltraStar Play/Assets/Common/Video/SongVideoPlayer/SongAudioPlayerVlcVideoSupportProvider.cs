using System;
using UniInject;
using UniRx;

public class SongAudioPlayerVlcVideoSupportProvider : AbstractVlcVideoSupportProvider
{
    [Inject]
    private SongAudioPlayer songAudioPlayer;

    public override bool IsSupported(string videoUri, SongMeta songMeta)
    {
        return base.IsSupported(videoUri, songMeta)
            && songMeta.Audio == songMeta.Video
            && songAudioPlayer.VlcMediaPlayer != null;
    }

    public override IObservable<VideoLoadedEvent> LoadVideoAsObservable(string videoUri)
    {
        return Observable.Create<VideoLoadedEvent>(o =>
        {
            StartCoroutine(CoroutineUtils.ExecuteWhenConditionIsTrue(
                () => songAudioPlayer.VlcMediaPlayer != null
                      && songAudioPlayer.VlcMediaPlayer.Media != null
                      && songAudioPlayer.VlcMediaPlayer.Media.Duration > 0,
                () => o.OnNext(new VideoLoadedEvent(videoUri))));
            return Disposable.Empty;
        });
    }

    public override void UnloadVideo()
    {
        // Handled by SongAudioPlayer
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

    public override float PlaybackSpeed
    {
        get => 1;
        set { /* Not available */ }
    }

    public override double PositionInVideoInMillis
    {
        get => songAudioPlayer.PositionInSongInMillis;
        set => songAudioPlayer.PositionInSongInMillis = value;
    }

    public override double DurationInMillis => songAudioPlayer.DurationOfSongInMillis;
}
