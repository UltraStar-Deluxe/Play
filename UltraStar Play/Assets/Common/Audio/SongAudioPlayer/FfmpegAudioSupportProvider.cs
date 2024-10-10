using System;
using System.IO;
using FfmpegUnity;
using UniInject;
using UniRx;
using UnityEngine;

public class FfmpegAudioSupportProvider : AbstractAudioSupportProvider
{
    [InjectedInInspector]
    public FfplayCommand ffplayCommandPrefab;

    private FfplayCommand ffplayCommand;
    private FfmpegPlayerVideoTexture ffmpegPlayerVideoTexture;

    private RenderTexture ffmpegRenderTexture;
    public RenderTexture FfmpegRenderTexture
    {
        get => ffmpegRenderTexture;
        set
        {
            ffmpegRenderTexture = value;
            if (ffplayCommand != null
                && ffplayCommand.VideoTexture != null)
            {
                ffplayCommand.VideoTexture.VideoTexture = ffmpegRenderTexture;
            }
        }
    }

    public override IObservable<AudioLoadedEvent> LoadAsObservable(string audioUri, bool streamAudio, double startPositionInMillis)
    {
        DestroyFfmpegPlayer();

        ffplayCommand = Instantiate(ffplayCommandPrefab, transform);
        ffmpegPlayerVideoTexture = ffplayCommand.GetComponentInChildren<FfmpegPlayerVideoTexture>();
        ffplayCommand.InputPath = audioUri;
        if (FfmpegRenderTexture != null
            && ffplayCommand.VideoTexture != null)
        {
            ffplayCommand.VideoTexture.VideoTexture = FfmpegRenderTexture;
        }

        // Play to trigger loading
        ffplayCommand.Play();
        PositionInMillis = startPositionInMillis;

        return Observable.Create<AudioLoadedEvent>(o =>
        {
            StartCoroutine(CoroutineUtils.ExecuteWhenConditionIsTrue(
                () => this == null || DurationInMillis > 0,
                () =>
                {
                    if (this == null)
                    {
                        string errorMessage = $"Failed to load audio clip '{audioUri}': {nameof(FfmpegAudioSupportProvider)} has been destroyed already.";
                        Debug.LogError(errorMessage);
                        throw new AudioSupportProviderException(errorMessage);
                    }

                    if (ffplayCommand == null)
                    {
                        o.OnError(new SongAudioPlayerException("Failed to load file with ffmpeg. FfplayCommand is null"));
                        return;
                    }

                    if (IsPlaying)
                    {
                        ffplayCommand.TogglePause();
                    }
                    o.OnNext(new AudioLoadedEvent(audioUri));
                }));
            return Disposable.Empty;
        });
    }

    public override bool IsSupported(string audioUri)
    {
        return !WebViewUtils.CanHandleWebViewUrl(audioUri)
            && settings.FfmpegToPlayMediaFilesUsage is not EThirdPartyLibraryUsage.Never
            && (ApplicationUtils.IsFfmpegSupportedAudioFormat(Path.GetExtension(audioUri))
                || ApplicationUtils.IsFfmpegSupportedVideoFormat(Path.GetExtension(audioUri)));
    }

    public override void Unload()
    {
        DestroyFfmpegPlayer();
    }

    public override void Play()
    {
        if (ffplayCommand == null)
        {
            return;
        }

        if (ffplayCommand.Paused)
        {
            ffplayCommand.TogglePause();
        }
    }

    public override void Pause()
    {
        if (ffplayCommand == null)
        {
            return;
        }

        if (ffplayCommand.IsRunning
            && !ffplayCommand.Paused)
        {
            ffplayCommand.TogglePause();
        }
    }

    public override void Stop()
    {
        if (ffplayCommand == null)
        {
            return;
        }

        ffplayCommand.Stop();
    }

    public override bool IsPlaying
    {
        get => ffplayCommand != null && ffplayCommand.IsRunning && !ffplayCommand.Paused;
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
        get => ffplayCommand.CurrentTime * 1000.0;
        // TODO: SeekTime is inaccurate, notably in the song editor.
        set => ffplayCommand.SeekTime(value / 1000.0);
    }

    public override double DurationInMillis => ffplayCommand != null
        ? ffplayCommand.Duration * 1000.0
        : 0;

    public override double VolumeFactor
    {
        get => ffplayCommand.AudioSourceComponent.volume;
        set => ffplayCommand.AudioSourceComponent.volume = (float)value;
    }

    private void DestroyFfmpegPlayer()
    {
        Destroy(FfmpegRenderTexture);
        DestroyFfmpegCommand();
    }

    private void DestroyFfmpegCommand()
    {
        if (ffplayCommand == null)
        {
            return;
        }

        if (ffplayCommand.AudioSourceComponent != null)
        {
            ffplayCommand.AudioSourceComponent.mute = true;
        }
        ffplayCommand.gameObject.SetActive(false);
        if (Application.isEditor && !Application.isPlaying)
        {
            DestroyImmediate(ffplayCommand.gameObject);
        }
        else
        {
            Destroy(ffplayCommand.gameObject);
        }
        ffplayCommand = null;
    }
}
