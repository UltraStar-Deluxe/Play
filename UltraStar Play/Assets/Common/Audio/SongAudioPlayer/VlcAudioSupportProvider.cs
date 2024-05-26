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

    private long lastVlcMediaPlayerTimeWhenPlaying;
    private float lastAudioListenerVolume;
    private double lastSetVolumeFactor = 1;

    private void Update()
    {
        if (IsPlaying)
        {
            lastVlcMediaPlayerTimeWhenPlaying = vlcMediaPlayer.Time;
        }

        // Update volume when AudioListener.volume changes
        if (Math.Abs(AudioListener.volume - lastAudioListenerVolume) > 0.01f)
        {
            lastAudioListenerVolume = AudioListener.volume;
            // AudioListener.volume is considered as part of the property setter
            VolumeFactor = lastSetVolumeFactor;
        }
    }

    public override IObservable<AudioLoadedEvent> LoadAsObservable(string audioUri, bool streamAudio, double startPositionInMillis)
    {
        if (vlcMediaPlayer == null)
        {
            vlcMediaPlayer = vlcManager.CreateMediaPlayer();
            VolumeFactor = lastSetVolumeFactor;
        }
        else
        {
            vlcMediaPlayer.Stop();
        }

        if (vlcMediaPlayer.Media != null)
        {
            vlcMediaPlayer.Media.Dispose();
        }

        vlcMediaPlayer.Media = new Media(new Uri(audioUri));

        // Play to trigger loading
        Play();
        PositionInMillis = startPositionInMillis;

        // The video is loaded asynchronously.
        // The duration property indicates whether it has been loaded.
        return Observable.Create<AudioLoadedEvent>(o =>
        {
            StartCoroutine(CoroutineUtils.ExecuteWhenConditionIsTrue(
                () => this == null || DurationInMillis > 0,
                () =>
                {
                    if (this == null)
                    {
                        string errorMessage = $"Failed to load audio clip '{audioUri}': {nameof(VlcAudioSupportProvider)} has been destroyed already.";
                        Debug.LogError(errorMessage);
                        throw new AudioSupportProviderException(errorMessage);
                    }

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
        if (vlcMediaPlayer == null
            || vlcMediaPlayer.IsPlaying)
        {
            return;
        }

        vlcMediaPlayer.Play();
    }

    public override void Pause()
    {
        if (vlcMediaPlayer == null
            || !vlcMediaPlayer.IsPlaying)
        {
            return;
        }

        vlcMediaPlayer.Pause();
    }

    public override void Stop()
    {
        if (vlcMediaPlayer == null)
        {
            return;
        }

        vlcMediaPlayer.Stop();
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
            return vlcMediaPlayer.IsPlaying
                ? vlcMediaPlayer.Time
                : lastVlcMediaPlayerTimeWhenPlaying;
        }

        // VLC MediaPlayer jumps to the end of the song when time is 0, so set 1 as minimum.
        set => vlcMediaPlayer.SetTime((long)Math.Max(1, value));
    }

    public override double DurationInMillis => vlcMediaPlayer?.Length ?? 0;

    public override double VolumeFactor
    {
        get => lastSetVolumeFactor;
        set
        {
            lastSetVolumeFactor = value;
            vlcMediaPlayer?.SetVolume((int)(value * 100.0 * AudioListener.volume));
        }
    }

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
