using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FullSerializer;

// Holds all in-memory stats data
[Serializable]
public class Statistics
{
    public float TotalPlayTimeSeconds { get; private set; }
    public Dictionary<string, LocalStatistic> LocalStatistics { get; private set; } = new Dictionary<string, LocalStatistic>();
    public Dictionary<string, WebStatistic> WebStatistics { get; private set; } = new Dictionary<string, WebStatistic>();
    public List<TopEntry> TopTenList { get; private set; } = new List<TopEntry>();
    public TopEntry TopScore { get; private set; }

    // Indicates whether the Statistics have non-persisted changes.
    // The flag is checked by the StatsManager, e.g., on scene change.
    // The flag is reset by the StatsManger on save.
    [fsIgnore]
    public bool IsDirty { get; set; }

    public LocalStatistic GetLocalStats(SongMeta songMeta)
    {
        LocalStatistics.TryGetValue(songMeta.SongHash, out LocalStatistic result);
        return result;
    }

    public WebStatistic GetWebStats(SongMeta songMeta)
    {
        WebStatistics.TryGetValue(songMeta.SongHash, out WebStatistic result);
        return result;
    }

    public void UpdateTotalPlayTime()
    {
        TotalPlayTimeSeconds += Time.realtimeSinceStartup;
    }

    public void RecordSongStarted(SongMeta songMeta)
    {
        LocalStatistics.GetOrInitialize(songMeta.SongHash).IncrementSongStarted();
        IsDirty = true;
    }

    public void RecordSongFinished(SongMeta songMeta, List<SongStatistic> songStatistics)
    {
        Debug.Log("Recording song finished stats for: " + songMeta.Title);
        LocalStatistic localStatistic = LocalStatistics.GetOrInitialize(songMeta.SongHash);
        localStatistic.IncrementSongFinished();
        foreach (SongStatistic songStatistic in songStatistics)
        {
            localStatistic.AddSongStatistics(songStatistic);
            UpdateTopScores(songMeta, songStatistic);
        }

        IsDirty = true;
    }

    private void UpdateTopScores(SongMeta songMeta, SongStatistic songStatistic)
    {
        Debug.Log("Updating top scores");
        TopEntry topEntry = new TopEntry(songMeta.Title, songMeta.Artist, songStatistic);

        //Update the top score
        if (TopScore == null || songStatistic.Score > TopScore.SongStatistic.Score)
        {
            TopScore = topEntry;
        }

        //Update the top ten
        //Find where in the current top ten to place the current score
        int topTenIndex = -1;
        for (int i = 0; i < TopTenList.Count; ++i)
        {
            if (songStatistic.Score > TopTenList[i].SongStatistic.Score)
            {
                topTenIndex = i + 1;
                break;
            }
        }

        //List isn't full yet, just add the score to the end
        if (topTenIndex == -1 || TopTenList.Count < 10)
        {
            TopTenList.Add(topEntry);
        }
        else
        {
            //should be fine. an oversight
            //Otherwise just insert the top score into its respective place
            TopTenList.Insert(topTenIndex, topEntry);
        }

        //Remove any scores beyond the top ten in the list
        if (TopTenList.Count > 10)
        {
            TopTenList = TopTenList.Take(10).ToList();
        }
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
