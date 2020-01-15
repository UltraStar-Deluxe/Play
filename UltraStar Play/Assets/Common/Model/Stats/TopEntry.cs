using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Holds data about one of the "top" songs
[Serializable]
public class TopEntry
{
    public string SongName { get; private set; }
    public string SongArtist { get; private set; }
    public SongStatistic SongStatistic { get; private set; }

    public TopEntry(string songName, string songArtist, SongStatistic songStatistic)
    {
        this.SongName = songName;
        this.SongArtist = songArtist;
        this.SongStatistic = songStatistic;
    }
}

//Comparer for score sorting
public class CompareByTopEntryScore : IComparer<TopEntry>
{
    public int Compare(TopEntry x, TopEntry y)
    {
        if (x.SongStatistic == null || y.SongStatistic == null)
        {
            return 0;
        }

        return x.SongStatistic.Score.CompareTo(y.SongStatistic.Score);
    }
}
