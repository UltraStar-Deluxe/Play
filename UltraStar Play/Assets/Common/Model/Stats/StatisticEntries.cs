using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using ProTrans;
using UnityEngine;

// Handles storage and operation over the song statistic entries themselves
[Serializable]
public class StatisticEntries
{
    public SortedSet<SongStatistic> SongStatistics { get; private set; }

    public StatisticEntries()
    {
        SongStatistics = new SortedSet<SongStatistic>(new CompareBySongScoreDescending());
    }

    public void AddRecord(SongStatistic record)
    {
        SongStatistics.Add(record);
    }

    public void RemoveRecord(SongStatistic record)
    {
        SongStatistics.Remove(record);
    }

    public List<SongStatistic> GetTopScores(int count)
    {
        if (SongStatistics.IsNullOrEmpty())
        {
            return new List<SongStatistic>();
        }
        return SongStatistics
            .Take(count)
            .ToList();
    }
}
