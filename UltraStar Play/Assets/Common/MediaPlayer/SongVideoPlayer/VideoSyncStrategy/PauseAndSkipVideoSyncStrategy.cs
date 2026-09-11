using System;
using System.Collections.Generic;
using CircularBuffer;
using UnityEngine;

/**
 * Uses pause and position change to keep SongVideoPlayer in sync with SongAudioPlayer.
 * Thereby, pause is assumed to react fast, whereas position change is assumed to take a small amount of time, such that it might not result in perfect sync.
 * This video sync strategy is an alternative for file formats where playback speed cannot be adjusted smoothly.
 */
public class PauseAndSkipVideoSyncStrategy : AbstractVideoSyncStrategy
{
    private const int ImmediatePlaybackPositionSyncThresholdInMillis = 2000;
    private const int MinOffsetToSyncThresholdInMillis = 100;
    private const double SyncCheckIntervalInSeconds = 2;
    private const float ResidualOffsetMeasurementDelayInSeconds = 0.5f;
    
    // Keep a rolling median of recent residual offsets observed after performing a seek.
    // Positive residual means target (audio-aligned) is ahead of the video after the seek
    // and next skip should be larger by that amount to compensate.
    private readonly CircularBuffer<double> residualHistoryInMillis = new(5);

    private bool syncingPositionWithPause;
    private float lastSyncTimeInSeconds;

    public override void SyncPlayPause(SongVideoPlayer songVideoPlayer)
    {
        if (syncingPositionWithPause)
        {
            return;
        }

        base.SyncPlayPause(songVideoPlayer);
    }

    public override void SyncPosition(SongVideoPlayer songVideoPlayer, bool forceImmediateSync)
    {
        if (!songVideoPlayer.IsFullyLoaded
            || (!forceImmediateSync && Time.time < lastSyncTimeInSeconds + SyncCheckIntervalInSeconds))
        {
            return;
        }
        lastSyncTimeInSeconds = Time.time;

        if (songVideoPlayer.IsWaitingForVideoGap())
        {
            Log.WithMethodContext().Verbose(() => "Waiting for video gap");
            return;
        }

        // Positive when target (audio aligned video time) is ahead of current video time
        double offsetInMillis = songVideoPlayer.GetOffsetPositionInMillis();
        if (!forceImmediateSync && Math.Abs(offsetInMillis) < MinOffsetToSyncThresholdInMillis)
        {
            Log.WithMethodContext().Verbose(() => $"No sync to audio position. Offset: {offsetInMillis:F1} ms");
            return;
        }

        // A big mismatch is corrected immediately.
        // A short mismatch is either paused or skipped to reach target position.
        // This is a workaround because unfortunately, SetRate did not work well to smooth out the difference.
        if (forceImmediateSync || Math.Abs(offsetInMillis) > ImmediatePlaybackPositionSyncThresholdInMillis)
        {
            Log.WithMethodContext().Verbose(() => $"Hard sync to audio position. Offset: {offsetInMillis:F1} ms");
            songVideoPlayer.PositionInMillis = songVideoPlayer.GetTargetPositionInMillis();
        }
        else if (offsetInMillis < 0)
        {
            Log.WithMethodContext().Verbose(() => $"Target is behind, pausing a moment. Offset: {offsetInMillis:F1} ms");
            SoftSyncViaPause(songVideoPlayer, offsetInMillis);
        }
        else if (offsetInMillis > 0)
        {
            Log.WithMethodContext().Verbose(() => $"Target is ahead, skipping a moment. Offset: {offsetInMillis:F1} ms");
            SoftSyncViaSkip(songVideoPlayer, songVideoPlayer.PositionInMillis, offsetInMillis);
        }
    }

    private async void SoftSyncViaPause(SongVideoPlayer songVideoPlayer, double offsetInMillis)
    {
        try
        {
            syncingPositionWithPause = true;
            songVideoPlayer.PauseVideo();
            // Offset is negative because video is ahead (larger time) of audio (smaller time).
            float waitTimeSeconds = (float)Math.Abs(offsetInMillis / 1000.0);
            Debug.Log($"Pause: {waitTimeSeconds} s");
            
            await Awaitable.WaitForSecondsAsync(waitTimeSeconds);
            
            // Check if should be playing because the state can have changed in the meantime.
            if (!songVideoPlayer.FreezeVideo
                && (songVideoPlayer.songAudioPlayer == null || songVideoPlayer.songAudioPlayer.IsPlaying)
                && !songVideoPlayer.IsWaitingForVideoGap())
            {
                songVideoPlayer.PlayVideo();
            }
        }
        finally
        {
            syncingPositionWithPause = false;
        }
    }

    private void SoftSyncViaSkip(SongVideoPlayer songVideoPlayer, double currentTime, double offsetInMillis)
    {
        // Use the learned median residual to compensate the seek. If residual was positive previously,
        // it means we ended up behind target, so add it to the skip.
        double compensationMs = GetMedianResidualInMillis();
        double skipTimeMs = compensationMs > 0
            ? offsetInMillis + compensationMs
            : offsetInMillis;
        double newPosition = currentTime + skipTimeMs;
        Log.WithMethodContext().Verbose(() => $"skip time: {skipTimeMs:F1} ms, offset: {offsetInMillis:F1} ms, residual compensation: {compensationMs:F1} ms");

        songVideoPlayer.PositionInMillis = newPosition;

        MeasureResidualOffsetAfterDelay(songVideoPlayer);
    }

    private void MeasureResidualOffsetAfterDelay(SongVideoPlayer songVideoPlayer)
    {
        // Measure residual offset after seek has finished. 
        AwaitableUtils.ExecuteAfterDelayInSecondsAsync(songVideoPlayer.gameObject, ResidualOffsetMeasurementDelayInSeconds, () =>
        {
            if (songVideoPlayer.gameObject == null)
            {
                // Object was destroyed in the meantime
                return;
            }
            
            double residualNowInMillis = songVideoPlayer.GetOffsetPositionInMillis();
            residualHistoryInMillis.PushBack(residualNowInMillis);

            Log.WithMethodContext().Verbose(() =>
                $"Measured post-seek residual: {residualNowInMillis:F1} ms (median: {GetMedianResidualInMillis():F1} ms)");
        });
    }

    private double GetMedianResidualInMillis()
    {
        if (residualHistoryInMillis.Count == 0)
        {
            return 0;
        }

        return NumberUtils.Median(residualHistoryInMillis.ToList());
    }
}
