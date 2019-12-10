using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Handles storage and operation over the song statistic entries themselves
[Serializable]
public class StatisticEntries
{
    public SortedSet<SongStatistic> songStatistics { get; private set; }

    public StatisticEntries()
    {
        songStatistics = new SortedSet<SongStatistic>(new CompareBySongScore());
    }

    public void AddRecord(SongStatistic record)
    {
        songStatistics.Add(record);
    }

    public void RemoveRecord(SongStatistic record)
    {
        songStatistics.Remove(record);
    }
}
