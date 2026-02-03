public interface IVideoSyncStrategy
{
    void SyncPlayPause(SongVideoPlayer songVideoPlayer);
    void SyncPosition(SongVideoPlayer songVideoPlayer, bool forceImmediateSync);
    // Called frequently (e.g., every frame) to allow strategies to perform measurements
    // or background work without forcing sync actions.
    void Update(SongVideoPlayer songVideoPlayer);
}
