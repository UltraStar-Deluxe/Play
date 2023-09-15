using System;

public interface IHighscoreConnector : IMod
{
    public IObservable<HighScoreRecord> ReadHighScoreRecord(SongMeta songMeta);
    public void WriteHighScoreRecord(SongMeta songMeta);
}
