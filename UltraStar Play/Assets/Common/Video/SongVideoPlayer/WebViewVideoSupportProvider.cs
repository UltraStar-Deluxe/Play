using System;
using UniInject;
using UniRx;
using UnityEngine;

public class WebViewVideoSupportProvider : AbstractVideoSupportProvider
{
    [Inject]
    private WebViewManager webViewManager;

    public override bool IsSupported(string videoUri, SongMeta songMeta)
    {
        return WebViewUtils.CanHandleWebViewUrl(videoUri);
    }

    public override IObservable<VideoLoadedEvent> LoadVideoAsObservable(string videoUri)
    {
        return Observable.Create<VideoLoadedEvent>(o =>
        {
            StartCoroutine(CoroutineUtils.ExecuteWhenConditionIsTrue(
                () => webViewManager.DurationInMillis > 0,
                () => o.OnNext(new VideoLoadedEvent(videoUri))));
            return Disposable.Empty;
        });
    }

    public override void UnloadVideo()
    {
        ResetWebViewRenderTexture();
    }

    public override void PlayVideo()
    {
        webViewManager.ResumePlayback();
    }

    public override void PauseVideo()
    {
        webViewManager.PausePlayback();
    }

    public override void StopVideo()
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

    public override float PlaybackSpeed
    {
        get => 1;
        set { /* Not available */ }
    }

    public override double PositionInVideoInMillis
    {
        get => webViewManager.EstimatedPlaybackPositionInMillis;
        set => webViewManager.SetPlaybackPositionInMillis(value);
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
