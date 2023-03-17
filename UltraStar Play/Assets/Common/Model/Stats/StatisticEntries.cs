using System;
using System.Collections.Generic;
using System.Linq;

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

    public List<SongStatistic> GetTopScores(int count, EDifficulty difficulty)
    {
        if (SongStatistics.IsNullOrEmpty())
        {
            return new List<SongStatistic>();
        }
        return SongStatistics
            .Where(it => it.Difficulty == difficulty)
            .Take(count)
            .ToList();
    }
}
