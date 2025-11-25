using System;
using System.IO;
using LibVLCSharp;
using UniInject;
using UnityEngine;

public class VlcAudioSupportProvider : AbstractAudioSupportProvider
{
    [Inject]
    private VlcManager vlcManager;

    private MediaPlayer vlcMediaPlayer;
    public MediaPlayer VlcMediaPlayer => vlcMediaPlayer;

    private double lastSetVolumeFactor = 1;

    private void Update()
    {
        // Update volume when AudioListener.volume changes, which is considered as part of the property setter
        if (IsPlaying
            && Math.Abs(VlcMediaPlayerTargetVolumePercent - vlcMediaPlayer.Volume) > 1)
        {
            VolumeFactor = lastSetVolumeFactor;
        }
    }

    public override async Awaitable<AudioLoadedEvent> LoadAsync(string audioUri, bool streamAudio, double startPositionInMillis)
    {
        if (vlcMediaPlayer == null)
        {
            vlcMediaPlayer = vlcManager.CreateMediaPlayer();
        }
        else
        {
            vlcMediaPlayer.StopAsync();
        }

        if (vlcMediaPlayer.Media != null)
        {
            vlcMediaPlayer.Media.Dispose();
        }

        vlcMediaPlayer.Media = new Media(new Uri(audioUri));

        // Set volume to 0 to avoid audio glitches. Needed because PlayAsync is used to trigger loading.
        vlcMediaPlayer.SetVolume(0);
        lastSetVolumeFactor = 0;

        // Play to trigger loading. PlayAsync to not block the main thread and avoid stutter.
        vlcMediaPlayer.PlayAsync();

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

    public override bool IsSupported(string audioUri)
    {
        return !WebViewUtils.CanHandleWebViewUrl(audioUri)
            && settings.VlcToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Never
            && (ApplicationUtils.IsVlcSupportedAudioFormat(Path.GetExtension(audioUri))
                || ApplicationUtils.IsVlcSupportedVideoFormat(Path.GetExtension(audioUri)));
    }

    public override void Unload()
    {
        DestroyVlcMediaPlayer();
    }

    public override void Play()
    {
        if (IsFullyLoaded)
        {
            vlcMediaPlayer?.SetPause(false);
        }
    }

    public override void Pause()
    {
        if (IsFullyLoaded)
        {
            vlcMediaPlayer?.SetPause(true);
        }
    }

    public override void Stop()
    {
        vlcMediaPlayer?.StopAsync();
    }

    public override bool IsPlaying
    {
        get => vlcMediaPlayer != null && vlcMediaPlayer.IsPlaying;
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
            return vlcMediaPlayer?.Time ?? 0;
        }

        set
        {
            // VLC MediaPlayer jumps to the end of the song when time is 0, so set 1 as minimum.
            if (IsFullyLoaded)
            {
                vlcMediaPlayer.SetTime((long)Math.Max(1, value));
            }
        }
    }

    public override double DurationInMillis => vlcMediaPlayer?.Length ?? 0;

    public override double VolumeFactor
    {
        get => lastSetVolumeFactor;
        set
        {
            lastSetVolumeFactor = value;
            if (IsPlaying)
            {
                vlcMediaPlayer?.SetVolume(VlcMediaPlayerTargetVolumePercent);
            }
        }
    }

    private int VlcMediaPlayerTargetVolumePercent => (int)(lastSetVolumeFactor * 100.0 * AudioListener.volume);

    private void DestroyVlcMediaPlayer()
    {
        if (vlcMediaPlayer == null)
        {
            return;
        }

        VlcManager.DestroyMediaPlayer(vlcMediaPlayer);
        vlcMediaPlayer = null;
    }
}
