public interface IVideoSyncStrategy
{
    void SyncPlayPause(SongVideoPlayer songVideoPlayer);
    void SyncPosition(SongVideoPlayer songVideoPlayer, bool forceImmediateSync);
}
