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

    public override IObservable<AudioLoadedEvent> LoadAsObservable(string audioUri, bool streamAudio, double startPositionInMillis)
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

        // Play to trigger loading. PlayAsync to not block the main thread and avoid stutter.
        vlcMediaPlayer.SetVolume(0);
        vlcMediaPlayer.PlayAsync();
        PositionInMillis = startPositionInMillis;

        // The video is loaded asynchronously.
        // The duration property indicates whether it has been loaded.
        return Observable.Create<AudioLoadedEvent>(o =>
        {
            StartCoroutine(CoroutineUtils.ExecuteWhenConditionIsTrue(
                () => this == null || IsFullyLoaded,
                () =>
                {
                    if (this == null)
                    {
                        string errorMessage = $"Failed to load audio clip '{audioUri}': {nameof(VlcAudioSupportProvider)} has been destroyed already.";
                        Debug.LogError(errorMessage);
                        throw new AudioSupportProviderException(errorMessage);
                    }

                    vlcMediaPlayer.SetPause(!shouldBePlaying);
                    o.OnNext(new AudioLoadedEvent(audioUri));
                }));
            return Disposable.Empty;
        });
    }

    public override bool IsSupported(string audioUri)
    {
        return !WebViewUtils.CanHandleWebViewUrl(audioUri)
            && settings.VlcToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Never
            && (ApplicationUtils.IsFfmpegSupportedAudioFormat(Path.GetExtension(audioUri))
                || ApplicationUtils.IsFfmpegSupportedVideoFormat(Path.GetExtension(audioUri)));
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

        // VLC MediaPlayer jumps to the end of the song when time is 0, so set 1 as minimum.
        set
        {
            lastVlcMediaPlayerTimeInMillisWhenPlaying = (long)value;
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
