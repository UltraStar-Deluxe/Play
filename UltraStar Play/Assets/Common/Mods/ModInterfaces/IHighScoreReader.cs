using UnityEngine;

public interface IHighScoreReader : IMod
{
    public Awaitable<HighScoreRecord> ReadHighScoreRecordAsync(SongMeta songMeta);
}
