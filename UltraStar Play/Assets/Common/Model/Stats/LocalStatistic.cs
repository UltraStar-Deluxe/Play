using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Stores offline statistics for a single song
[Serializable]
public class LocalStatistic
{
    public UInt32 TimesStarted { get; private set; }
    public UInt32 TimesFinished { get; private set; }
    public DateTime LastPlayed { get; private set; } = DateTime.MinValue;
    public StatisticEntries StatsEntries { get; private set; } = new StatisticEntries();

    //Called whenever a song is started
    public void UpdateSongStarted()
    {
        ++TimesStarted;
        LastPlayed = DateTime.Now;
    }

    //Called whenever a song is finished
    public void UpdateSongFinished(SongStatistic songStatistic)
    {
        ++TimesFinished;
        LastPlayed = DateTime.Now;
        StatsEntries.AddRecord(songStatistic);
    }
}