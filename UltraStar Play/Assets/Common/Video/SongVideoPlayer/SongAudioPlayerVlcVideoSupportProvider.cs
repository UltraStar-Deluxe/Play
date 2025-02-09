using System;
using LibVLCSharp;
using UniInject;
using UniRx;
using UnityEngine;

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

    public override async Awaitable<VideoLoadedEvent> LoadAsync(string videoUri, double startPositionInMillis)
    {
        await ConditionUtils.WaitForConditionAsync(() => !this || SongAudioPlayerVlcMediaPlayer?.Media?.Duration > 0,
            new WaitForConditionConfig { description = "libVLC MediaPlayer has loaded audio with valid duration" });
        if (!this)
        {
            throw new DestroyedAlreadyException($"Failed to load video '{videoUri}': {nameof(SongAudioPlayerVlcVideoSupportProvider)} has been destroyed already.");
        }

        mediaPlayer = SongAudioPlayerVlcMediaPlayer;
        return new VideoLoadedEvent(videoUri);
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
