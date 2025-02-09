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

    public override async Awaitable<VideoLoadedEvent> LoadAsync(string videoUri, double startPositionInMillis)
    {
        await ConditionUtils.WaitForConditionAsync(() => !this || webViewManager.DurationInMillis > 0,
            new WaitForConditionConfig {description = $"load video '{videoUri}'", timeoutInMillis = 30000});
        if (!this)
        {
            throw new DestroyedAlreadyException($"Failed to load video '{videoUri}': {nameof(WebViewVideoSupportProvider)} has been destroyed already.");
        }

        return new VideoLoadedEvent(videoUri);
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
