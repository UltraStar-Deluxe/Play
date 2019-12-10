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
    public float totalPlayTimeSeconds { get; private set; } = 0;
    public Dictionary<string, LocalStatistic> localStatistics { get; private set; } = new Dictionary<string, LocalStatistic>();
    public Dictionary<string, WebStatistic> webStatistics { get; private set; } = new Dictionary<string, WebStatistic>();
    public List<TopEntry> topTenList { get; private set; } = new List<TopEntry>();
    public TopEntry topScore { get; private set; } = null;

    public void UpdateTotalPlayTime()
    {
        totalPlayTimeSeconds += Time.realtimeSinceStartup;
    }

    public void RecordSongStarted(SongMeta songMeta)
    {
        GetOrInitialize<LocalStatistic>(localStatistics, songMeta.SongHash).UpdateSongStarted();
        TriggerDatabaseWrite();
    }

    public void RecordSongFinished(SongMeta songMeta, string playerName, EDifficulty difficulty, int score)
    {
        Debug.Log("Recording song stats for " + playerName);
        var statsObject = new SongStatistic(playerName, difficulty, score);
        GetOrInitialize<LocalStatistic>(localStatistics, songMeta.SongHash).UpdateSongFinished(statsObject);

        //todo: if web scores are enabled
        //set website here?
        GetOrInitialize<WebStatistic>(webStatistics, songMeta.SongHash).UpdateSongFinished(statsObject);

        UpdateTopScores(songMeta, statsObject);
       
        TriggerDatabaseWrite();
    }

    private void UpdateTopScores(SongMeta songMeta, SongStatistic songStatistic)
    {
        Debug.Log("Updating top scores");
        var topEntry = new TopEntry(songMeta.Title, songMeta.Artist, songStatistic);
        
        //Update the top score
        if (topScore == null || songStatistic.score > topScore.songStatistic.score)
            topScore = topEntry;

        //Update the top ten
        //Find where in the current top ten to place the current score
        var topTenIndex = -1;
        for (int i = 0; i < topTenList.Count; ++i)
        {
            if (songStatistic.score > topTenList[i].songStatistic.score)
            {
                topTenIndex = i + 1;
                break;
            }
        }

        //List isn't full yet, just add the score to the end
        if (topTenIndex == -1 && topTenList.Count < 10)
            topTenList.Add(topEntry);
        else
            //Otherwise just insert the top score into its respective place
            topTenList.Insert(topTenIndex, topEntry);

        //Remove any scores beyond the top ten in the list
        if (topTenList.Count > 10)
            topTenList = topTenList.Take(10).ToList();
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
