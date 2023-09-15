using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class HighScoreRecord
{
    public SortedSet<HighScoreEntry> HighScoreEntries { get; private set; }

    public HighScoreRecord()
    {
        HighScoreEntries = new SortedSet<HighScoreEntry>(new HighScoreEntry.CompareByScoreDescending());
    }

    public void AddRecord(HighScoreEntry record)
    {
        HighScoreEntries.Add(record);
    }

    public void RemoveRecord(HighScoreEntry record)
    {
        HighScoreEntries.Remove(record);
    }
}
