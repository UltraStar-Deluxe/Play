using System;
using System.Collections.Generic;
using System.Linq;

// Handles storage and operation over the song statistic entries themselves
[Serializable]
public class HighScoreRecord
{
    public SortedSet<HighScoreEntry> HighScoreEntries { get; private set; }

    public HighScoreRecord()
    {
        HighScoreEntries = new SortedSet<HighScoreEntry>(new CompareBySongScoreDescending());
    }

    public void AddRecord(HighScoreEntry record)
    {
        HighScoreEntries.Add(record);
    }

    public void RemoveRecord(HighScoreEntry record)
    {
        HighScoreEntries.Remove(record);
    }

    public List<HighScoreEntry> GetTopScores(int count, EDifficulty difficulty)
    {
        if (HighScoreEntries.IsNullOrEmpty())
        {
            return new List<HighScoreEntry>();
        }
        return HighScoreEntries
            .Where(it => it.Difficulty == difficulty)
            .Take(count)
            .ToList();
    }
}
