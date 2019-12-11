using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Handles storage and operation over the song statistic entries themselves
[Serializable]
public class StatisticEntries
{
    public SortedSet<SongStatistic> SongStatistics { get; private set; }

    //Returns the top score entry or null if no scores are yet recorded
    public SongStatistic TopScore
    {
        get
        {
            return SongStatistics.FirstOrDefault();
        }
    }

    public StatisticEntries()
    {
        SongStatistics = new SortedSet<SongStatistic>(new CompareBySongScore());
    }

    public void AddRecord(SongStatistic record)
    {
        SongStatistics.Add(record);
    }

    public void RemoveRecord(SongStatistic record)
    {
        SongStatistics.Remove(record);
    }
}
