using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Stores web statistics for a single song
[Serializable]
public class WebStatistic
{
    public string webSite { get; private set; } = "";
    public StatisticEntries statsEntries { get; private set; } = new StatisticEntries();

    public void SetWebSite(string webSite)
    {
        this.webSite = webSite;
    }

    public void UpdateSongFinished(SongStatistic songStatistic)
    {
        statsEntries.AddRecord(songStatistic);
    }
}
