using System;
using LibVLCSharp;
using UniInject;
using UnityEngine;

public class VlcAudioSupportProvider : AbstractAudioSupportProvider
{
    [Inject]
    private VlcManager vlcManager;

    private MediaPlayer mediaPlayer;
    public MediaPlayer MediaPlayer => mediaPlayer;

    private double lastSetVolumeFactor = 1;

    private void Update()
    {
        // Update volume when AudioListener.volume changes, which is considered as part of the property setter
        if (IsPlaying
            && Math.Abs(VlcMediaPlayerTargetVolumePercent - mediaPlayer.Volume) > 1)
        {
            VolumeFactor = lastSetVolumeFactor;
        }
    }

    public override async Awaitable<AudioLoadedEvent> LoadAsync(string audioUri, bool streamAudio, double startPositionInMillis)
    {
        if (mediaPlayer == null)
        {
            mediaPlayer = vlcManager.CreateMediaPlayer();
        }
        else
        {
            mediaPlayer.StopAsync();
        }

        if (mediaPlayer.Media != null)
        {
            mediaPlayer.Media.Dispose();
        }

        mediaPlayer.Media = new Media(new Uri(audioUri));

        // Set volume to 0 to avoid audio glitches. Needed because PlayAsync is used to trigger loading.
        mediaPlayer.SetVolume(0);
        lastSetVolumeFactor = 0;

        // Play to trigger loading. PlayAsync to not block the main thread and avoid stutter.
        mediaPlayer.PlayAsync();

        // Only set PositionInMillis if not 0, to avoid unnecessary time changes. This avoids audio glitches and time synchronization mismatches.
        if (startPositionInMillis > 0)
        {
            PositionInMillis = startPositionInMillis;
        }

        // Wait until media has been loaded asynchronously.
        await ConditionUtils.WaitForConditionAsync(() => !this || IsFullyLoaded,
            new WaitForConditionConfig {description = $"load audio '{audioUri}'" });
        if (!this)
        {
            throw new DestroyedAlreadyException($"Failed to load audio clip '{audioUri}': {nameof(VlcAudioSupportProvider)} has been destroyed already.");
        }

        return new AudioLoadedEvent(audioUri);
    }

    public override void Unload()
    {
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
        get => mediaPlayer != null && mediaPlayer.IsPlaying;
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

    public override double PlaybackSpeed
    {
        get => 1;
        set => SetPlaybackSpeed(value, true);
    }

    public override void SetPlaybackSpeed(double newValue, bool changeTempoButKeepPitch)
    {
        // Not supported
    }

    public override double PositionInMillis
    {
        get
        {
            return mediaPlayer?.Time ?? 0;
        }

        set
        {
            // VLC MediaPlayer jumps to the end of the song when time is 0, so set 1 as minimum.
            if (IsFullyLoaded)
            {
                mediaPlayer.SetTime((long)Math.Max(1, value));
            }
        }
    }

    public override double DurationInMillis => mediaPlayer?.Length ?? 0;

    public override double VolumeFactor
    {
        get => lastSetVolumeFactor;
        set
        {
            lastSetVolumeFactor = value;
            if (IsPlaying)
            {
                mediaPlayer?.SetVolume(VlcMediaPlayerTargetVolumePercent);
            }
        }
    }

    private int VlcMediaPlayerTargetVolumePercent => (int)(lastSetVolumeFactor * 100.0 * AudioListener.volume);

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
