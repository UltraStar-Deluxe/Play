using System;
using System.Collections.Generic;
using System.Linq;
using FullSerializer;
using UnityEngine;

/**
 * Data structure for song scores.
 */
[Serializable]
public class Statistics
{
    public float TotalPlayTimeSeconds { get; set; }
    public Dictionary<string, SongStatistics> LocalStatistics { get; private set; } = new();

    // Indicates whether the Statistics have non-persisted changes.
    // The flag is checked by the StatsManager, e.g., on scene change.
    // The flag is reset by the StatsManger on save.
    [fsIgnore]
    public bool IsDirty { get; set; }

    public SongStatistics GetLocalStatistics(SongMeta songMeta)
    {
        if (songMeta == null)
        {
            return null;
        }

        string scoreRelevantSongHash = SongMetaManager.GetAndCacheScoreRelevantHash(songMeta);
        LocalStatistics.TryGetValue(scoreRelevantSongHash, out SongStatistics result);
        return result;
    }

    public HighScoreEntry GetLocalHighScore(SongMeta songMeta, EDifficulty difficulty)
    {
        SongStatistics songStatistics = GetLocalStatistics(songMeta);
        if (songStatistics == null
            || songStatistics.HighScoreRecord == null
            || songStatistics.HighScoreRecord.HighScoreEntries.IsNullOrEmpty())
        {
            return null;
        }

        HighScoreEntry highScoresEntry = songStatistics.HighScoreRecord.GetTopScores(1, difficulty).FirstOrDefault();
        return highScoresEntry;
    }

    public int GetLocalHighscore(SongMeta songMeta, EDifficulty difficulty)
    {
        HighScoreEntry highScoreEntry = GetLocalHighScore(songMeta, difficulty);
        if (highScoreEntry == null)
        {
            return 0;
        }
        return highScoreEntry.Score;
    }
    
    public void RecordSongStarted(SongMeta songMeta)
    {
        SongStatistics songStatistics = CreateLocalStatistics(songMeta);
        songStatistics.IncrementSongStarted();

        IsDirty = true;
    }

    public void RecordSongFinished(SongMeta songMeta, List<HighScoreEntry> highScoreEntries)
    {
        Debug.Log("Recording song finished stats for: " + songMeta.Title);
        SongStatistics songStatistics = CreateLocalStatistics(songMeta);
        songStatistics.IncrementSongFinished();
        foreach (HighScoreEntry highScoreEntry in highScoreEntries)
        {
            songStatistics.AddHighScore(highScoreEntry);
        }

        IsDirty = true;
    }

    private SongStatistics CreateLocalStatistics(SongMeta songMeta)
    {
        string scoreRelevantSongHash = SongMetaManager.GetAndCacheScoreRelevantHash(songMeta);
        SongStatistics songStatistics = LocalStatistics.GetOrInitialize(scoreRelevantSongHash);
        songStatistics.SongArtist = songMeta.Artist;
        songStatistics.SongTitle = songMeta.Title;
        return songStatistics;
    }
    
    public bool HasHighscore(SongMeta songMeta)
    {
        SongStatistics localStatistics = GetLocalStatistics(songMeta);
        return localStatistics != null
            && localStatistics.HighScoreRecord != null
            && localStatistics.HighScoreRecord.HighScoreEntries != null
            && localStatistics.HighScoreRecord.HighScoreEntries.Count > 0;
    }
}
