using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Represents a single song statistic entry
[Serializable]
public class SongStatistic
{
    public string PlayerName { get; private set; }
    public EDifficulty Difficulty { get; private set; }
    public int Score { get; private set; }
    public DateTime DateTime { get; private set; }

    public SongStatistic(string playerName, EDifficulty difficulty, int score)
    {
        this.PlayerName = playerName;
        this.Difficulty = difficulty;
        this.Score = score;
        this.DateTime = DateTime.Now;
    }
}

//Comparer for score sorting
public class CompareBySongScore : IComparer<SongStatistic>
{
    public int Compare(SongStatistic x, SongStatistic y)
    {
        return x.Score.CompareTo(y.Score);
    }
}