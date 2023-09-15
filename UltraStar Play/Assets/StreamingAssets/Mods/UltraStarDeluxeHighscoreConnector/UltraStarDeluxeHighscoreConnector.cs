using System;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;

public class UltraStarDeluxeHighscoreConnector : IHighscoreConnector
{
    public IObservable<HighScoreRecord> ReadHighScoreRecord(SongMeta songMeta)
    {
        Debug.Log($"Searching USDX highscore database for song '{SongMetaUtils.GetArtistDashTitle(songMeta)}'");
        return ObservableUtils.RunOnNewTaskAsObservable(
            () => ReadHighScoreRecordAsync(songMeta),
            Disposable.Empty);
    }

    private async Task<HighScoreRecord> ReadHighScoreRecordAsync(SongMeta songMeta)
    {
        Debug.Log($"Simulating slow USDX DB");
        ThreadUtils.Sleep(1000);
        
        HighScoreRecord highScoreRecord = new HighScoreRecord();
        highScoreRecord.AddRecord(new HighScoreEntry(
            "Player X",
            EDifficulty.Medium,
            7777,
            EScoreMode.Individual,
            "UltraStar Deluxe Database"));

        highScoreRecord.AddRecord(new HighScoreEntry(
            "Player Y",
            EDifficulty.Medium,
            8888,
            EScoreMode.Individual,
            "UltraStar Deluxe Database"));

        return highScoreRecord;
    }

    public void WriteHighScoreRecord(SongMeta songMeta)
    {
        // Not implemented yet.
        return;
    }
}