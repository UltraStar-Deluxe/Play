using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Represents a single song statistic entry
[Serializable]
public class SongStatistic
{
    public string playerName { get; private set; } = "";
    public EDifficulty difficulty { get; private set; } = EDifficulty.Easy;
    public int score { get; private set; } = 0;
    public DateTime dateTime { get; private set; } = DateTime.MinValue;

    public SongStatistic(string playerName, EDifficulty difficulty, int score)
    {
        this.playerName = playerName;
        this.difficulty = difficulty;
        this.score = score;
        this.dateTime = DateTime.Now;
    }
}

//Comparer for score sorting
public class CompareBySongScore : IComparer<SongStatistic>
{
    public int Compare(SongStatistic a, SongStatistic b)
    {
        return a.score.CompareTo(b.score);
    }
}