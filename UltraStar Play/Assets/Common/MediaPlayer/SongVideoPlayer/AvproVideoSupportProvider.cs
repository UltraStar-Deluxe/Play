using RenderHeads.Media.AVProVideo;
using UnityEngine;

public class AvproVideoSupportProvider : AbstractAvproVideoSupportProvider
{
    public MediaPlayer mediaPlayer;
    public ResolveToRenderTexture resolveToRenderTexture;

    private string lastMediaPlayerError = "";
    
    public override bool IsSupported(string videoUri, bool videoEqualsAudio)
    {
        return base.IsSupported(videoUri, videoEqualsAudio)
               // The SongAudioPlayer's mediaPlayer should be used when video and audio are equal
               && !videoEqualsAudio;
    }

    public override void Unload()
    {
        mediaPlayer.Stop();
        mediaPlayer.CloseMedia();
        resolveToRenderTexture.ExternalTexture = null;
    }

    public override async Awaitable<VideoLoadedEvent> LoadAsync(string videoUri, double startPositionInMillis)
    {
        mediaPlayer.Stop();
        mediaPlayer.CloseMedia();

        mediaPlayer.Events.RemoveAllListeners();
        mediaPlayer.Events.AddListener(OnMediaPlayerError);
        lastMediaPlayerError = "";

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
        resolveToRenderTexture.ExternalTexture = renderTexture;
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
    
    private void OnMediaPlayerError(MediaPlayer aMediaPlayer, MediaPlayerEvent.EventType eventType, ErrorCode errorCode)
    {
        if (errorCode is ErrorCode.None)
        {
            return;
        }
        lastMediaPlayerError = Helper.GetErrorMessage(errorCode);
    }

    private bool HasMediaPlayerError()
    {
        return !lastMediaPlayerError.IsNullOrEmpty();
    }
}
