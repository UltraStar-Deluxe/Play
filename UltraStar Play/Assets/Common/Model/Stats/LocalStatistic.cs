using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Stores offline statistics for a single song
[Serializable]
public class LocalStatistic
{
    public UInt32 timesStarted { get; private set; } = 0;
    public UInt32 timesFinished { get; private set; } = 0;
    public DateTime lastPlayed { get; private set; } = DateTime.MinValue;
    public StatisticEntries statsEntries { get; private set; } = new StatisticEntries();

    //Called whenever a song is started
    public void UpdateSongStarted()
    {
        ++timesStarted;
        lastPlayed = DateTime.Now;
    }

    //Called whenever a song is finished
    public void UpdateSongFinished(SongStatistic songStatistic)
    {
        ++timesFinished;
        lastPlayed = DateTime.Now;
        statsEntries.AddRecord(songStatistic);
    }
}