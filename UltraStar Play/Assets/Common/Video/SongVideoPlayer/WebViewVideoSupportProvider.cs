using System;
using UniInject;
using UniRx;
using UnityEngine;

public class WebViewVideoSupportProvider : AbstractVideoSupportProvider
{
    [Inject]
    private WebViewManager webViewManager;

    public override bool IsSupported(string videoUri, bool videoEqualsAudio)
    {
        return WebViewUtils.CanHandleWebViewUrl(videoUri);
    }

    public override IObservable<VideoLoadedEvent> LoadAsObservable(string videoUri, double startPositionInMillis)
    {
        return Observable.Create<VideoLoadedEvent>(o =>
        {
            StartCoroutine(CoroutineUtils.ExecuteWhenConditionIsTrue(
                () => webViewManager.DurationInMillis > 0,
                () => o.OnNext(new VideoLoadedEvent(videoUri))));
            return Disposable.Empty;
        });
    }

    public override void Unload()
    {
        ResetWebViewRenderTexture();
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
                webViewManager.ResumePlayback();
            }
            else
            {
                webViewManager.PausePlayback();
            }
        }
    }

    public override bool IsLooping
    {
        get => false;
        set { /* Not available */ }
    }

    public override double PlaybackSpeed
    {
        get => 1;
        set { /* Not available */ }
    }

    public override double PositionInMillis
    {
        get => webViewManager.EstimatedPositionInMillis;
        set => webViewManager.SetPositionInMillis(value);
    }

    public override double DurationInMillis => webViewManager.DurationInMillis;

    public override void SetTargetTexture(RenderTexture renderTexture)
    {
        if (renderTexture != null)
        {
            SetWebViewRenderTextureToVideoRenderTexture(renderTexture);
        }
        else
        {
            ResetWebViewRenderTexture();
        }
    }

    private void ResetWebViewRenderTexture()
    {
        webViewManager.ResetWebViewRenderTexture();
    }

    private void SetWebViewRenderTextureToVideoRenderTexture(RenderTexture renderTexture)
    {
        webViewManager.SetWebViewRenderTexture(renderTexture);
    }
}
