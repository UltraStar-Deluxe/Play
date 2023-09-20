using System;

public interface IHighScoreReader : IMod
{
    public IObservable<HighScoreRecord> ReadHighScoreRecord(SongMeta songMeta);
}
