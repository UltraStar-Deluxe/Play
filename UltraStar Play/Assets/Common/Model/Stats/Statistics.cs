using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Holds all in-memory stats data
[Serializable]
public class Statistics
{
    //Stats entries are static to persist across scenes
    public float TotalPlayTimeSeconds { get; private set; }
    public Dictionary<string, LocalStatistic> LocalStatistics { get; private set; } = new Dictionary<string, LocalStatistic>();
    public Dictionary<string, WebStatistic> WebStatistics { get; private set; } = new Dictionary<string, WebStatistic>();
    public List<TopEntry> TopTenList { get; private set; } = new List<TopEntry>();
    public TopEntry TopScore { get; private set; }

    public LocalStatistic GetLocalStats(SongMeta songMeta)
    {
        LocalStatistic result = null;
        LocalStatistics.TryGetValue(songMeta.SongHash, out result);
        return result;
    }

    public WebStatistic GetWebStats(SongMeta songMeta)
    {
        WebStatistic result = null;
        WebStatistics.TryGetValue(songMeta.SongHash, out result);
        return result;
    }

    public void UpdateTotalPlayTime()
    {
        TotalPlayTimeSeconds += Time.realtimeSinceStartup;
    }

    public void RecordSongStarted(SongMeta songMeta)
    {
        GetOrInitialize<LocalStatistic>(LocalStatistics, songMeta.SongHash).UpdateSongStarted();
        TriggerDatabaseWrite();
    }

    public void RecordSongFinished(SongMeta songMeta, string playerName, EDifficulty difficulty, int score)
    {
        Debug.Log("Recording song stats for " + playerName);
        SongStatistic statsObject = new SongStatistic(playerName, difficulty, score);
        GetOrInitialize<LocalStatistic>(LocalStatistics, songMeta.SongHash).UpdateSongFinished(statsObject);

        UpdateTopScores(songMeta, statsObject);

        TriggerDatabaseWrite();
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

    private void TriggerDatabaseWrite()
    {
        StatsManager.Instance.Save();
    }

    private T GetOrInitialize<T>(Dictionary<string, T> dict, string key) where T : new()
    {
        if (!dict.TryGetValue(key, out _))
        {
            dict[key] = new T();
        }
        return dict[key];
    }
}
