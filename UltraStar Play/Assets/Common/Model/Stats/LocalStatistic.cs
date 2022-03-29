using System;

//Stores offline statistics for a single song
[Serializable]
public class LocalStatistic
{
    public int TimesStarted { get; private set; }
    public int TimesFinished { get; private set; }
    public int TimesCanceled => TimesStarted - TimesFinished;
    public DateTime LastPlayed { get; private set; } = DateTime.MinValue;
    public StatisticEntries StatsEntries { get; private set; } = new StatisticEntries();

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
    public void AddSongStatistics(SongStatistic songStatistic)
    {
        StatsEntries.AddRecord(songStatistic);
    }
}
