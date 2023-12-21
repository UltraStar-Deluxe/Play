using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using UniInject;
using UniRx;
using UnityEngine;

public class UltraStarDeluxeHighscoreConnector : IHighScoreReader, IOnDisableMod
{
    private Dictionary<SongMeta, HighScoreRecord> songMetaToHighScoreRecordCache = new Dictionary<SongMeta, HighScoreRecord>();

    [Inject]
    private UltraStarDeluxeHighscoreConnectorModSettings modSettings;

    private IDbConnection dbConnection;
    private IDbConnection DbConnection
    {
        get
        {
            if (dbConnection == null)
            {
                if (!FileUtils.Exists(modSettings.dbPath))
                {
                    throw new Exception($"UltraStar database file does not exist: {modSettings.dbPath}");
                }

                dbConnection = SqliteUtils.OpenSqliteConnectionToFile(modSettings.dbPath);
            }
            return dbConnection;
        }
    }

    public IObservable<HighScoreRecord> ReadHighScoreRecord(SongMeta songMeta)
    {
        if (songMeta == null
            || modSettings.dbPath.IsNullOrEmpty())
        {
            return Observable.Empty<HighScoreRecord>();
        }

        if (songMetaToHighScoreRecordCache.TryGetValue(songMeta, out HighScoreRecord cachedHighScoreRecord))
        {
            return Observable.Return<HighScoreRecord>(cachedHighScoreRecord);
        }

        Debug.Log($"Searching USDX highscore database for song '{SongMetaUtils.GetArtistDashTitle(songMeta)}'");
        return ObservableUtils.RunOnNewTaskAsObservable(
            async () =>
            {
                HighScoreRecord highScoreRecord = await ReadHighScoreRecordAsync(songMeta);
                if (highScoreRecord == null)
                {
                    songMetaToHighScoreRecordCache[songMeta] = new HighScoreRecord();
                }
                else 
                {
                    songMetaToHighScoreRecordCache[songMeta] = highScoreRecord;
                }
                return highScoreRecord;
            },
            Disposable.Empty);
    }

    private async Task<HighScoreRecord> ReadHighScoreRecordAsync(SongMeta songMeta)
    {
        string artistEscaped = EscapeSqlStringArgument(songMeta.Artist);
        string titleEscaped = EscapeSqlStringArgument(songMeta.Title);
        string sql = $"SELECT us_scores.songid, us_scores.player, us_scores.difficulty, us_scores.score " +
                $"FROM us_scores " +
                $"INNER JOIN us_songs ON us_songs.id=us_scores.songid " +
                $"WHERE us_songs.artist LIKE '{artistEscaped}' AND us_songs.title LIKE '{titleEscaped}' ";
        Log.Verbose(() => $"Executing SQL: {sql}");
        IDataReader dbReader = DbConnection.ExecuteQuery(sql);

        List<ScoreRecordQueryData> dbRecords = dbReader.ToList<ScoreRecordQueryData>();
        if (dbRecords.IsNullOrEmpty())
        {
            Log.Verbose(() => $"No USDX high score found for '{SongMetaUtils.GetArtistDashTitle(songMeta)}'");
            return null;
        }

        Log.Verbose(() => $"Found USDX high scores found for '{SongMetaUtils.GetArtistDashTitle(songMeta)}': {dbRecords.Count}, {JsonConverter.ToJson(dbRecords)}");
        HighScoreRecord highScoreRecord = new HighScoreRecord();
        foreach(ScoreRecordQueryData dbRecord in dbRecords)
        {
            highScoreRecord.AddRecord(ToHighScoreEntry(dbRecord));
        }

        return highScoreRecord;
    }

    private string EscapeSqlStringArgument(string text)
    {
        return text.Replace("'", "''");
    }

    private HighScoreEntry ToHighScoreEntry(ScoreRecordQueryData dbRecord)
    {
        return new HighScoreEntry(
            dbRecord.Player,
            ToDifficultyEnum(dbRecord.Difficulty),
            (int)dbRecord.Score,
            EScoreMode.Individual,
            "UltraStar Deluxe Database");
    }

    private EDifficulty ToDifficultyEnum(long difficulty)
    {
        switch (difficulty)
        {
            case 0: return EDifficulty.Easy;
            case 1: return EDifficulty.Medium;
            case 2: return EDifficulty.Hard;
            default: return EDifficulty.Medium;
        }
    }

    public void WriteHighScoreRecord(SongMeta songMeta)
    {
        // TODO: implement.
        return;
    }

    public void OnDisableMod()
    {
        if (dbConnection != null)
        {
            dbConnection.Dispose();
            dbConnection = null;
        }
    }

    public class ScoreRecordQueryData
    {
        public long SongID;
        public string Player;
        public long Difficulty;
        public long Score;
    }
}