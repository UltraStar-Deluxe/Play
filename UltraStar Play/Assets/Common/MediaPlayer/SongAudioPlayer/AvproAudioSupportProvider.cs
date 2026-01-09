using System;
using System.IO;
using RenderHeads.Media.AVProVideo;
using UnityEngine;

public class AvproAudioSupportProvider : AbstractAudioSupportProvider
{
    public MediaPlayer mediaPlayer;

    private double lastSetVolumeFactor = 1;

    private void Update()
    {
        // Update volume when AudioListener.volume changes, which is considered as part of the property setter
        if (IsPlaying
            && Math.Abs(MediaPlayerTargetVolumePercent - mediaPlayer.AudioVolume) > 1)
        {
            VolumeFactor = lastSetVolumeFactor;
        }
    }

    public override async Awaitable<AudioLoadedEvent> LoadAsync(string audioUri, bool streamAudio, double startPositionInMillis)
    {
        mediaPlayer.Pause();
        mediaPlayer.CloseMedia();


        // Set volume to 0 to avoid audio glitches.
        mediaPlayer.AudioVolume = 0;
        lastSetVolumeFactor = 0;

        // Play to trigger loading. autoPlay is true by default
        mediaPlayer.OpenMedia(new MediaPath(audioUri, MediaPathType.AbsolutePathOrURL));

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
            throw new DestroyedAlreadyException($"Failed to load audio clip '{audioUri}': {nameof(AvproAudioSupportProvider)} has been destroyed already.");
        }

        return new AudioLoadedEvent(audioUri);
    }

    public override bool IsSupported(string audioUri)
    {
        return !WebViewUtils.CanHandleWebViewUrl(audioUri)
            && settings.AvProToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Never
            && (ApplicationUtils.IsAvproSupportedAudioFormat(Path.GetExtension(audioUri))
                || ApplicationUtils.IsAvproSupportedVideoFormat(Path.GetExtension(audioUri)));
    }

    public override void Unload()
    {
    }

    public override void Play()
    {
        if (IsFullyLoaded)
        {
            mediaPlayer.Play();
        }
    }

    public override void Pause()
    {
        if (IsFullyLoaded)
        {
            mediaPlayer.Pause();
        }
    }

    public override void Stop()
    {
        mediaPlayer.Pause();
        mediaPlayer.CloseMedia();
    }

    public override bool IsPlaying
    {
        get => mediaPlayer.Control.IsPlaying();
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
        get => mediaPlayer.PlaybackRate;
        set => SetPlaybackSpeed(value, true);
    }

    public override void SetPlaybackSpeed(double newValue, bool changeTempoButKeepPitch)
    {
        mediaPlayer.PlaybackRate = (float)newValue;
    }

    public override double PositionInMillis
    {
        get
        {
            return (mediaPlayer.Control?.GetCurrentTime() ?? 0) * 1000.0;
        }

        set
        {
            if (IsFullyLoaded)
            {
                mediaPlayer.Control?.SeekFast(value / 1000.0);
            }
        }
    }

    public override double DurationInMillis => (mediaPlayer.Info?.GetDuration() ?? 0) * 1000.0;

    public override double VolumeFactor
    {
        get => lastSetVolumeFactor;
        set
        {
            lastSetVolumeFactor = value;
            if (IsPlaying)
            {
                mediaPlayer.AudioVolume = MediaPlayerTargetVolumePercent;
            }
        }
    }

    private float MediaPlayerTargetVolumePercent => (float)(lastSetVolumeFactor * AudioListener.volume);
}
