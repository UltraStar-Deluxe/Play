using RenderHeads.Media.AVProVideo;
using UnityEngine;

public class AvproVideoSupportProvider : AbstractAvproVideoSupportProvider
{
    public MediaPlayer mediaPlayer;
    public ResolveToRenderTexture resolveToRenderTexture;

    protected override bool IsFullyLoaded => base.IsFullyLoaded && firstFrameReady;
    
    private string lastMediaPlayerError = "";

    private bool firstFrameReady;
    private double startPositionInMillis;
    
    private RenderTexture TargetTexture
    {
        get => resolveToRenderTexture.ExternalTexture;
        set => resolveToRenderTexture.ExternalTexture = value;
    }
    
    public override void Unload()
    {
        mediaPlayer.Stop();
        mediaPlayer.CloseMedia();
        RenderTextureUtils.Clear(TargetTexture);
        SetTargetTexture(null);
        firstFrameReady = false;
    }

    public override async Awaitable<VideoLoadedEvent> LoadAsync(string videoUri, double startPositionInMillis)
    {
        mediaPlayer.Stop();
        mediaPlayer.CloseMedia();

        mediaPlayer.Events.RemoveAllListeners();
        mediaPlayer.Events.AddListener(OnMediaPlayerEvent);
        lastMediaPlayerError = "";
        firstFrameReady = false;
        this.startPositionInMillis = startPositionInMillis;

        // autoPlay is true by default
        mediaPlayer.OpenMedia(new MediaPath(videoUri, MediaPathType.AbsolutePathOrURL));
        mediaPlayer.AudioMuted = true;
        
        if (startPositionInMillis > 0)
        {
            PositionInMillis = startPositionInMillis;
        }

        // The video is loaded asynchronously.
        // The duration property indicates whether it has been loaded.
        await ConditionUtils.WaitForConditionAsync(() => !this || IsFullyLoaded || HasMediaPlayerError());
        if (!this)
        {
            throw new DestroyedAlreadyException($"Failed to load video '{videoUri}': {nameof(AvproVideoSupportProvider)} has been destroyed already.");
        }

        if (HasMediaPlayerError())
        {
            throw new AvproException(lastMediaPlayerError);
        }

        return new VideoLoadedEvent(videoUri);
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
        mediaPlayer.Stop();
    }

    public override void SetTargetTexture(RenderTexture renderTexture)
    {
        TargetTexture = renderTexture;
    }

    public override bool IsPlaying
    {
        get => mediaPlayer.Control?.IsPlaying() ?? false;
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

    public override bool IsLooping
    {
        get => mediaPlayer.Control?.IsLooping() ?? false;
        set { mediaPlayer.Control?.SetLooping(value); }
    }

    public override double PlaybackSpeed
    {
        get => mediaPlayer.Control?.GetPlaybackRate() ?? 1;
        set
        {
            mediaPlayer.Control?.SetPlaybackRate((float)value);
        }
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

    private void OnMediaPlayerEvent(MediaPlayer aMediaPlayer, MediaPlayerEvent.EventType eventType, ErrorCode errorCode)
    {
        switch (eventType)
        {
            case MediaPlayerEvent.EventType.ResolutionChanged when !firstFrameReady:
                // Seek to half of the video duration to trigger loading a frame.
                // Otherwise, the video does not load sometimes ( https://github.com/RenderHeads/UnityPlugin-AVProVideo/issues/2427 )
                mediaPlayer.Control?.SeekFast(mediaPlayer.Info.GetDuration() / 2);
                break;
            case MediaPlayerEvent.EventType.Error when errorCode is not ErrorCode.None:
                lastMediaPlayerError = Helper.GetErrorMessage(errorCode);
                break;
            case MediaPlayerEvent.EventType.FirstFrameReady:
                firstFrameReady = true;
                // Reset position to intended start position.
                PositionInMillis = startPositionInMillis;
                break;
        }
    }

    private bool HasMediaPlayerError()
    {
        return !lastMediaPlayerError.IsNullOrEmpty();
    }
}
