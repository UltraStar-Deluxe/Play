using RenderHeads.Media.AVProVideo;
using UniInject;
using UnityEngine;

public class SongAudioPlayerAvproVideoSupportProvider : AbstractAvproVideoSupportProvider
{
    [Inject]
    private SongAudioPlayer songAudioPlayer;

    private RenderTexture TargetTexture
    {
        get => songAudioPlayer.GetComponentInChildren<ResolveToRenderTexture>().ExternalTexture;
        set => songAudioPlayer.GetComponentInChildren<ResolveToRenderTexture>().ExternalTexture = value;
    }

    private MediaPlayer SongAudioPlayerAvproMediaPlayer
    {
        get
        {
            return songAudioPlayer.CurrentAudioSupportProvider is AvproAudioSupportProvider avproAudioSupportProvider
                ? avproAudioSupportProvider.mediaPlayer
                : null;
        }
    }

    public override async Awaitable<VideoLoadedEvent> LoadAsync(string videoUri, double startPositionInMillis)
    {
        await ConditionUtils.WaitForConditionAsync(() => !this || SongAudioPlayerAvproMediaPlayer?.Info?.GetDuration() > 0,
            new WaitForConditionConfig { description = "libVLC MediaPlayer has loaded audio with valid duration" });
        if (!this)
        {
            throw new DestroyedAlreadyException($"Failed to load video '{videoUri}': {nameof(SongAudioPlayerAvproVideoSupportProvider)} has been destroyed already.");
        }

        return new VideoLoadedEvent(videoUri);
    }

    public override void Unload()
    {
        RenderTextureUtils.Clear(TargetTexture);
        // Rest Handled by SongAudioPlayer
    }

    public override void Play()
    {
        // Handled by SongAudioPlayer
    }

    public override void Pause()
    {
        // Handled by SongAudioPlayer
    }

    public override void Stop()
    {
        // Handled by SongAudioPlayer
    }

    public override void SetTargetTexture(RenderTexture renderTexture)
    {
        TargetTexture = renderTexture;
    }

    public override bool IsPlaying
    {
        get => songAudioPlayer.IsPlaying;
        set
        {
            if (value)
            {
                songAudioPlayer.PlayAudio();
            }
            else
            {
                songAudioPlayer.PauseAudio();
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
        get => songAudioPlayer.PositionInMillis;
        set => songAudioPlayer.PositionInMillis = value;
    }

    public override double DurationInMillis => songAudioPlayer.DurationInMillis;
}
