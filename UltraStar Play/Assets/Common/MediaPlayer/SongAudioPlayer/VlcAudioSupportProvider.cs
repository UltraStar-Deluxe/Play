using System;
using System.IO;
using LibVLCSharp;
using UniInject;
using UniRx;
using UnityEngine;

public class VlcAudioSupportProvider : AbstractAudioSupportProvider
{
    [Inject]
    private VlcManager vlcManager;

    private MediaPlayer vlcMediaPlayer;
    public MediaPlayer VlcMediaPlayer => vlcMediaPlayer;

    private long lastVlcMediaPlayerTimeInMillisWhenPlaying;
    private double lastSetVolumeFactor = 1;

    private bool shouldBePlaying;

    private void Update()
    {
        if (shouldBePlaying && IsPlaying)
        {
            lastVlcMediaPlayerTimeInMillisWhenPlaying = vlcMediaPlayer.Time;
        }

        UpdateVlcMediaPlayerPause();

        // Update volume when AudioListener.volume changes, which is considered as part of the property setter
        if (shouldBePlaying
            && IsPlaying
            && Math.Abs(VlcMediaPlayerTargetVolumePercent - vlcMediaPlayer.Volume) > 1)
        {
            VolumeFactor = lastSetVolumeFactor;
        }
    }

    private void UpdateVlcMediaPlayerPause()
    {
        // TODO: Workaround for unreliable VLC MediaPlayer pause state ( https://code.videolan.org/videolan/vlc/-/issues/28353 )
        if (!IsFullyLoaded)
        {
            return;
        }

        if (!shouldBePlaying && vlcMediaPlayer != null && vlcMediaPlayer.IsPlaying)
        {
            Log.Verbose(() => "Should be paused but VLC MediaPlayer is playing. Set VLC MediaPlayer to pause again.");
            vlcMediaPlayer.SetPause(true);
            vlcMediaPlayer.SetVolume(0);
            PositionInMillis = lastVlcMediaPlayerTimeInMillisWhenPlaying;
        }
        else if (shouldBePlaying && vlcMediaPlayer != null && !vlcMediaPlayer.IsPlaying)
        {
            Log.Verbose(() => "Should be playing but VLC MediaPlayer is paused. Set VLC MediaPlayer to play again.");
            vlcMediaPlayer.SetPause(false);
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

        // Because of VLC bug with play/pause time, shouldBePlaying needs to be in sync initially.
        // Otherwise, the time sync workaround might restart the audio unexpectedly.
        // PlayAsync is used to trigger loading, so set shouldBePlaying to true to be in sync initially.
        shouldBePlaying = true;

        // Play to trigger loading. PlayAsync to not block the main thread and avoid stutter.
        vlcMediaPlayer.PlayAsync();

        // Avoid unnecessary time changes to avoid audio glitches and time synchronization mismatches.
        if (startPositionInMillis > 0)
        {
            PositionInMillis = startPositionInMillis;
        }

        // The video is loaded asynchronously.
        // The duration property indicates whether it has been loaded.
        await ConditionUtils.WaitForConditionAsync(() => !this || IsFullyLoaded,
            new WaitForConditionConfig {description = $"load audio '{audioUri}'" });
        if (!this)
        {
            throw new DestroyedAlreadyException($"Failed to load audio clip '{audioUri}': {nameof(VlcAudioSupportProvider)} has been destroyed already.");
        }

        vlcMediaPlayer.SetPause(!shouldBePlaying);
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
        shouldBePlaying = true;
        if (IsFullyLoaded)
        {
            vlcMediaPlayer?.SetPause(false);
        }
    }

    public override void Pause()
    {
        shouldBePlaying = false;
        if (IsFullyLoaded)
        {
            vlcMediaPlayer?.SetPause(true);
        }
    }

    public override void Stop()
    {
        shouldBePlaying = false;
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
            // VLC MediaPlayer continues time even if not playing. Workaround: return old time if not playing.
            return shouldBePlaying && vlcMediaPlayer.IsPlaying
                ? vlcMediaPlayer.Time
                : lastVlcMediaPlayerTimeInMillisWhenPlaying;
        }

        set
        {
            lastVlcMediaPlayerTimeInMillisWhenPlaying = (long)value;
            // VLC MediaPlayer jumps to the end of the song when time is 0, so set 1 as minimum.
            vlcMediaPlayer.SetTime((long)Math.Max(1, value));
        }
    }

    public override double DurationInMillis => vlcMediaPlayer?.Length ?? 0;

    public override double VolumeFactor
    {
        get => lastSetVolumeFactor;
        set
        {
            lastSetVolumeFactor = value;
            if (shouldBePlaying && IsPlaying)
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
