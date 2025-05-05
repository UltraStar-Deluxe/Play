using System;
using LibVLCSharp;
using UniInject;
using UniRx;
using UnityEngine;

public class VlcVideoSupportProvider : AbstractVlcVideoSupportProvider
{
    [Inject]
    private VlcManager vlcManager;

    private long lastVlcMediaPlayerTimeInMillisWhenPlaying;
    private bool shouldBePlaying;

    protected override void Update()
    {
        base.Update();

        if (shouldBePlaying && IsPlaying)
        {
            lastVlcMediaPlayerTimeInMillisWhenPlaying = mediaPlayer.Time;
        }

        UpdateVlcMediaPlayerPause();
    }

    private void UpdateVlcMediaPlayerPause()
    {
        // TODO: Workaround for unreliable VLC MediaPlayer pause state ( https://code.videolan.org/videolan/vlc/-/issues/28353 )
        if (!IsFullyLoaded)
        {
            return;
        }

        if (!shouldBePlaying && mediaPlayer != null && mediaPlayer.IsPlaying)
        {
            Log.Verbose(() => "Should be paused but VLC MediaPlayer is playing. Set VLC MediaPlayer to pause again.");
            mediaPlayer.SetPause(true);
            PositionInMillis = lastVlcMediaPlayerTimeInMillisWhenPlaying;
        }
        else if (shouldBePlaying && mediaPlayer != null && !mediaPlayer.IsPlaying)
        {
            Log.Verbose(() => "Should be playing but VLC MediaPlayer is paused. Set VLC MediaPlayer to play again.");
            mediaPlayer.SetPause(false);
            PositionInMillis = lastVlcMediaPlayerTimeInMillisWhenPlaying;
        }
    }

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
        await ConditionUtils.WaitForConditionAsync(() => !this || mediaPlayer?.Media?.Duration > 0);
        if (!this)
        {
            throw new DestroyedAlreadyException($"Failed to load video '{videoUri}': {nameof(VlcVideoSupportProvider)} has been destroyed already.");
        }

        mediaPlayer.SetPause(!shouldBePlaying);
        return new VideoLoadedEvent(videoUri);
    }

    public override void Unload()
    {
        base.Unload();
        DestroyVlcMediaPlayer();
    }

    public override void Play()
    {
        shouldBePlaying = true;
        if (IsFullyLoaded)
        {
            mediaPlayer?.SetPause(false);
        }
    }

    public override void Pause()
    {
        shouldBePlaying = false;
        if (IsFullyLoaded)
        {
            mediaPlayer?.SetPause(true);
        }
    }

    public override void Stop()
    {
        shouldBePlaying = true;
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
            // VLC MediaPlayer continues time even if not playing. Workaround: return old time if not playing.
            return shouldBePlaying && mediaPlayer.IsPlaying
                ? mediaPlayer.Time
                : lastVlcMediaPlayerTimeInMillisWhenPlaying;
        }
        set
        {
            lastVlcMediaPlayerTimeInMillisWhenPlaying = (long)value;
            mediaPlayer?.SetTime((long)value);
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
