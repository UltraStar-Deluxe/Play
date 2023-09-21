using System;

//Stores web statistics for a single song
[Serializable]
public class WebSongStatistics
{
    public string WebSite { get; private set; }
    public HighScoreRecord HighScoreRecord { get; private set; } = new();

    public void SetWebSite(string webSite)
    {
        this.WebSite = webSite;
    }

    public void UpdateSongFinished(HighScoreEntry highScoreEntry)
    {
        HighScoreRecord.AddRecord(highScoreEntry);
    }
}
