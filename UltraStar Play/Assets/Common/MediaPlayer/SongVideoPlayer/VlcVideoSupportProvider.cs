using System;
using LibVLCSharp;
using UniInject;
using UnityEngine;

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

    public override async Awaitable<VideoLoadedEvent> LoadAsync(string videoUri, double startPositionInMillis)
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
        PositionInMillis = startPositionInMillis;

        // The video is loaded asynchronously.
        // The duration property indicates whether it has been loaded.
        await ConditionUtils.WaitForConditionAsync(() => !this || IsFullyLoaded);
        if (!this)
        {
            throw new DestroyedAlreadyException($"Failed to load video '{videoUri}': {nameof(VlcVideoSupportProvider)} has been destroyed already.");
        }
        return new VideoLoadedEvent(videoUri);
    }

    public override void Unload()
    {
        base.Unload();
        DestroyVlcMediaPlayer();
    }

    public override void Play()
    {
        if (IsFullyLoaded)
        {
            mediaPlayer?.SetPause(false);
        }
    }

    public override void Pause()
    {
        if (IsFullyLoaded)
        {
            mediaPlayer?.SetPause(true);
        }
    }

    public override void Stop()
    {
        mediaPlayer?.StopAsync();
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
            return mediaPlayer?.Time ?? 0;;
        }
        set
        {
            if (IsFullyLoaded)
            {
                mediaPlayer?.SetTime((long)value);
            }
        }
    }

    public override double DurationInMillis => mediaPlayer?.Length ?? 0;

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
