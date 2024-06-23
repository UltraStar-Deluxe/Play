using System;
using UniInject;
using UniRx;
using UnityEngine;

public class WebViewAudioSupportProvider : AbstractAudioSupportProvider
{
    [Inject]
    private WebViewManager webViewManager;

    public override IObservable<AudioLoadedEvent> LoadAsObservable(string audioUri, bool streamAudio, double startPositionInMillis)
    {
        bool success = webViewManager.LoadUrl(audioUri);
        if (!success)
        {
            return ObservableUtils.LogExceptionThenThrow<AudioLoadedEvent>(
                new SongAudioPlayerException($"Failed to load audio via WebView with URL {audioUri}"));
        }

        // The WebView is loaded asynchronously. When the duration is available then the audio is loaded.
        long startTime = TimeUtils.GetUnixTimeMilliseconds();
        long timeoutInMillis = 30000;
        return Observable.Create<AudioLoadedEvent>(o =>
        {
            StartCoroutine(CoroutineUtils.ExecuteWhenConditionIsTrue(
                () => this == null
                      || DurationInMillis > 0
                      || TimeUtils.IsDurationAboveThresholdInMillis(startTime, timeoutInMillis),
                () =>
                {
                    if (this == null)
                    {
                        string errorMessage = $"Failed to load audio clip '{audioUri}': {nameof(WebViewAudioSupportProvider)} has been destroyed already.";
                        Debug.LogError(errorMessage);
                        throw new AudioSupportProviderException(errorMessage);
                    }

                    if (TimeUtils.IsDurationAboveThresholdInMillis(startTime, timeoutInMillis))
                    {
                        o.OnError(new AudioSupportProviderException("Loading audio using WebView timed out."));
                        return;
                    }

                    PositionInMillis = startPositionInMillis;
                    o.OnNext(new AudioLoadedEvent(audioUri));
                }));

            return Disposable.Empty;
        });
    }

    public override bool IsSupported(string audioUri)
    {
        return WebViewUtils.CanHandleWebViewUrl(audioUri);
    }

    public override void Unload()
    {
        webViewManager.StopPlayback();
    }

    public override void Play()
    {
        webViewManager.ResumePlayback();
    }

    public override void Pause()
    {
        webViewManager.PausePlayback();
    }

    public override void Stop()
    {
        webViewManager.StopPlayback();
    }

    public override bool IsPlaying
    {
        get => webViewManager.IsPlaying;
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
        get => webViewManager.EstimatedPositionInMillis;
        set => webViewManager.SetPositionInMillis(value);
    }

    public override double DurationInMillis => webViewManager.DurationInMillis;

    public override double VolumeFactor
    {
        get => webViewManager.VolumeInPercent / 100.0;
        set => webViewManager.VolumeInPercent = (int)(value * 100.0);
    }
}
