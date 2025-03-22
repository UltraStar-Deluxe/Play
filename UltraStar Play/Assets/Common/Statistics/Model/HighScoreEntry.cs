using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class HighScoreEntry
{
    public string PlayerName { get; private set; }
    public EDifficulty Difficulty { get; private set; }
    public EScoreMode ScoreMode { get; private set; }
    public int Score { get; private set; }
    public DateTime DateTime { get; private set; }

    /**
     * Identifies the remote source of a highscore entry,
     * for example the online server where it was found.
     */
    [JsonIgnore]
    public string RemoteSource { get; private set; }

    public HighScoreEntry(
        string playerName,
        EDifficulty difficulty,
        int score,
        EScoreMode scoreMode,
        string remoteSource = "")
    {
        this.PlayerName = playerName;
        this.Difficulty = difficulty;
        this.Score = score;
        this.DateTime = DateTime.Now;
        this.ScoreMode = scoreMode;
        this.RemoteSource = remoteSource;
    }

    public class CompareByScoreAscending : IComparer<HighScoreEntry>
    {
        public int Compare(HighScoreEntry x, HighScoreEntry y)
        {
            return x.Score.CompareTo(y.Score);
        }
    }

    public class CompareByScoreDescending: IComparer<HighScoreEntry>
    {
        public int Compare(HighScoreEntry x, HighScoreEntry y)
        {
            return -x.Score.CompareTo(y.Score);
        }
    }
}
