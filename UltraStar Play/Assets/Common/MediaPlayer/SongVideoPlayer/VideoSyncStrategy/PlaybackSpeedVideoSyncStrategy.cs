using System;
using UnityEngine;

/**
 * Uses playback speed adjustments and occasional seeking to keep SongVideoPlayer in sync with SongAudioPlayer.
 */
public class PlaybackSpeedVideoSyncStrategy : AbstractVideoSyncStrategy
{
    private const int ImmediatePlaybackPositionSyncThresholdInMillis = 3000;
    private const int MinOffsetToSyncThresholdInMillis = 10;
    private const float SyncCheckIntervalInSeconds = 1;
    
    private float lastSyncTimeInSeconds;
    
    public override void SyncPosition(SongVideoPlayer songVideoPlayer, bool forceImmediateSync)
    {
        if (!songVideoPlayer.IsFullyLoaded
            || (!forceImmediateSync && lastSyncTimeInSeconds > Time.time))
        {
            return;
        }
        lastSyncTimeInSeconds = Time.time + SyncCheckIntervalInSeconds;

        if (songVideoPlayer.IsWaitingForVideoGap())
        {
            Log.WithMethodContext().Verbose(() => "Waiting for video gap");
            return;
        }

        double offsetInMillis = songVideoPlayer.GetOffsetPositionInMillis();

        if (songVideoPlayer.FreezeVideo)
        {
            songVideoPlayer.PlaybackSpeed = 0;
            Log.WithMethodContext().Verbose(() => $"Freeze video via playback speed 0");
        }
        else
        {
            // A big mismatch is corrected immediately.
            // A short mismatch in video and song position is smoothed out by adjusting the playback speed of the video.
            if (forceImmediateSync || Math.Abs(offsetInMillis) > ImmediatePlaybackPositionSyncThresholdInMillis)
            {
                // Correct the mismatch immediately.
                Log.WithMethodContext().Verbose(() => $"Hard sync to audio position. Offset: {offsetInMillis:F1} ms");
                songVideoPlayer.PositionInMillis = songVideoPlayer.GetTargetPositionInMillis();
                songVideoPlayer.PlaybackSpeed = 1f;
            }
            else if (Math.Abs(offsetInMillis) < MinOffsetToSyncThresholdInMillis)
            {
                // Good enough, do not change anything.
                float newPlaybackSpeed = 1;
                Log.WithMethodContext().Verbose(() => $"No sync needed. Offset: {offsetInMillis:F1} ms, PlaybackSpeed: {newPlaybackSpeed}");
                songVideoPlayer.PlaybackSpeed = newPlaybackSpeed;
            }
            else
            {
                // Smooth out the time difference over a duration of 2 seconds
                float newPlaybackSpeed = 1 + (float)(offsetInMillis / 2000);
                Log.WithMethodContext().Verbose(() => $"Smooth sync via playback speed. Offset: {offsetInMillis:F1} ms, PlaybackSpeed: {newPlaybackSpeed}");
                songVideoPlayer.PlaybackSpeed = newPlaybackSpeed;
            }
        }
    }
}
