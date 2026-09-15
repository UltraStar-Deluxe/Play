public class PlayPauseOnlyVideoSyncStrategy : AbstractVideoSyncStrategy
{
    public override void SyncPosition(SongVideoPlayer songVideoPlayer, bool forceImmediateSync)
    {
        // Do not try to sync position.
    }
}
