using System;
using System.Collections.Generic;

[Serializable]
public class HighScoreEntry
{
    public string PlayerName { get; private set; }
    public EDifficulty Difficulty { get; private set; }
    public EScoreMode ScoreMode { get; private set; }
    public int Score { get; private set; }
    public DateTime DateTime { get; private set; }

    public HighScoreEntry(string playerName, EDifficulty difficulty, int score, EScoreMode scoreMode)
    {
        this.PlayerName = playerName;
        this.Difficulty = difficulty;
        this.Score = score;
        this.DateTime = DateTime.Now;
        this.ScoreMode = scoreMode;
    }
}

public class CompareBySongScoreAscending : IComparer<HighScoreEntry>
{
    public int Compare(HighScoreEntry x, HighScoreEntry y)
    {
        return x.Score.CompareTo(y.Score);
    }
}

public class CompareBySongScoreDescending: IComparer<HighScoreEntry>
{
    public int Compare(HighScoreEntry x, HighScoreEntry y)
    {
        return -x.Score.CompareTo(y.Score);
    }
}
