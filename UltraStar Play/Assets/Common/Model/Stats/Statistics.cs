using System;
using System.Collections.Generic;
using FullSerializer;
using UnityEngine;

/**
 * Data structure for song scores.
 */
[Serializable]
public class Statistics
{
    public float TotalPlayTimeSeconds { get; set; }
    public Dictionary<string, LocalStatistic> LocalStatistics { get; private set; } = new();
    public Dictionary<string, WebStatistic> WebStatistics { get; private set; } = new();

    // Indicates whether the Statistics have non-persisted changes.
    // The flag is checked by the StatsManager, e.g., on scene change.
    // The flag is reset by the StatsManger on save.
    [fsIgnore]
    public bool IsDirty { get; set; }

    public LocalStatistic GetLocalStats(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return null;
        }
        LocalStatistics.TryGetValue(songMeta.SongHash, out LocalStatistic result);
        return result;
    }

    public WebStatistic GetWebStats(SongMeta songMeta)
    {
        WebStatistics.TryGetValue(songMeta.SongHash, out WebStatistic result);
        return result;
    }

    public void RecordSongStarted(SongMeta songMeta)
    {
        LocalStatistic localStatistic = CreateLocalStatistics(songMeta);
        localStatistic.IncrementSongStarted();

        IsDirty = true;
    }

    public void RecordSongFinished(SongMeta songMeta, List<SongStatistic> songStatistics)
    {
        Debug.Log("Recording song finished stats for: " + songMeta.Title);
        LocalStatistic localStatistic = CreateLocalStatistics(songMeta);
        localStatistic.IncrementSongFinished();
        foreach (SongStatistic songStatistic in songStatistics)
        {
            localStatistic.AddSongStatistics(songStatistic);
        }

        IsDirty = true;
    }

    private LocalStatistic CreateLocalStatistics(SongMeta songMeta)
    {
        LocalStatistic localStatistic = LocalStatistics.GetOrInitialize(songMeta.SongHash);
        localStatistic.SongArtist = songMeta.Artist;
        localStatistic.SongTitle = songMeta.Title;
        return localStatistic;
    }
    
    public bool HasHighscore(SongMeta songMeta)
    {
        if (GetLocalStats(songMeta) != null)
        {
            return !GetLocalStats(songMeta).StatsEntries.SongStatistics.IsNullOrEmpty();
        }
        return false;
    }
}
