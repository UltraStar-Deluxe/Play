using System;

//Stores web statistics for a single song
[Serializable]
public class WebStatistic
{
    public string WebSite { get; private set; }
    public StatisticEntries StatsEntries { get; private set; } = new();

    public void SetWebSite(string webSite)
    {
        this.WebSite = webSite;
    }

    public void UpdateSongFinished(SongStatistic songStatistic)
    {
        StatsEntries.AddRecord(songStatistic);
    }
}
