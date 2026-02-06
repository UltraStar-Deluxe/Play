public abstract class AbstractVideoSyncStrategy : IVideoSyncStrategy
{
    public virtual void SyncPlayPause(SongVideoPlayer songVideoPlayer)
    {
        if (!songVideoPlayer.IsFullyLoaded
            || !songVideoPlayer.gameObject.activeInHierarchy)
        {
            return;
        }

        bool songAudioPlayerIsPlaying = songVideoPlayer.songAudioPlayer != null
                                        && songVideoPlayer.songAudioPlayer.IsPlaying;

        if (// Pause when audio is paused
            (!songAudioPlayerIsPlaying && songVideoPlayer.IsPlaying)
            // Pause when audio is beyond and video is not looping
            || (songVideoPlayer.IsFullyLoaded
                && songVideoPlayer.songAudioPlayer.PositionInSeconds >= songVideoPlayer.DurationInMillis 
                && !songVideoPlayer.IsLooping)
            // Pause when video is frozen
            || songVideoPlayer.FreezeVideo)
        {
            songVideoPlayer.PauseVideo();
        }
        else if ((songAudioPlayerIsPlaying && !songVideoPlayer.IsPlaying)
                 && !songVideoPlayer.IsWaitingForVideoGap())
        {
            songVideoPlayer.PlayVideo();
            SyncPosition(songVideoPlayer, true);
        }
    }

    public abstract void SyncPosition(SongVideoPlayer songVideoPlayer, bool forceImmediateSync);
}
