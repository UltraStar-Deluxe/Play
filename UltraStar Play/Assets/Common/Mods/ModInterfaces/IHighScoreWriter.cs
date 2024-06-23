public interface IHighScoreWriter : IMod
{
    public void WriteHighScoreRecord(HighScoreRecord highScoreRecord, SongMeta songMeta);
}
