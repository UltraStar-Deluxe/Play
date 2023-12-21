using System;
using System.Collections.Generic;

//Holds data about one of the "top" songs
[Serializable]
public class TopEntry
{
    public string SongName { get; private set; }
    public string SongArtist { get; private set; }
    public HighScoreEntry HighScoreEntry { get; private set; }

    public TopEntry(string songName, string songArtist, HighScoreEntry highScoreEntry)
    {
        this.SongName = songName;
        this.SongArtist = songArtist;
        this.HighScoreEntry = highScoreEntry;
    }
}

//Comparer for score sorting
public class CompareByTopEntryScore : IComparer<TopEntry>
{
    public int Compare(TopEntry x, TopEntry y)
    {
        if (x.HighScoreEntry == null || y.HighScoreEntry == null)
        {
            return 0;
        }

        return x.HighScoreEntry.Score.CompareTo(y.HighScoreEntry.Score);
    }
}
