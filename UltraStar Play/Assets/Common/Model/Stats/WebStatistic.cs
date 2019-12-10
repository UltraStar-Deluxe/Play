using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Stores web statistics for a single song
[Serializable]
public class WebStatistic
{
    public string WebSite { get; private set; }
    public StatisticEntries StatsEntries { get; private set; } = new StatisticEntries();

    public void SetWebSite(string webSite)
    {
        this.WebSite = webSite;
    }

    public void UpdateSongFinished(SongStatistic songStatistic)
    {
        StatsEntries.AddRecord(songStatistic);
    }
}
