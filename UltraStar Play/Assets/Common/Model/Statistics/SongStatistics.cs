using System;

/**
 * Stores offline statistics for a single song
 */
[Serializable]
public class SongStatistics
{
    public int TimesStarted { get; private set; }
    public int TimesFinished { get; private set; }
    public int TimesCanceled => TimesStarted - TimesFinished;
    public DateTime LastPlayed { get; private set; } = DateTime.MinValue;
    public HighScoreRecord HighScoreRecord { get; private set; } = new();
    public string SongArtist { get; set; }
    public string SongTitle { get; set; }

    // Called once when a song is started
    public void IncrementSongStarted()
    {
        TimesStarted++;
        LastPlayed = DateTime.Now;
    }

    // Called once when a song is finished
    public void IncrementSongFinished()
    {
        TimesFinished++;
        LastPlayed = DateTime.Now;
    }

    // Called for every player when a song is finished
    public void AddHighScore(HighScoreEntry highScoreEntry)
    {
        if (highScoreEntry.Score <= 0)
        {
            return;
        }
        
        HighScoreRecord.AddRecord(highScoreEntry);
    }
}
