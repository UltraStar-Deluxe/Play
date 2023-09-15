using System;
using UniRx;
using UnityEngine;

public class UltraStarDeluxeHighscoreConnector : IHighscoreConnector
{
    public IObservable<HighScoreRecord> ReadHighScoreRecord(SongMeta songMeta)
    {
        Debug.Log($"Searching USDX highscore database for song '{SongMetaUtils.GetArtistDashTitle(songMeta)}'");

        Subject<HighScoreRecord> subject = new Subject<HighScoreRecord>();
        ThreadPool.QueueUserWorkItem(_ => 
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

            Debug.Log($"Returning HighScoreRecord");
            subject.OnNext(highScoreRecord);
            subject.OnCompleted();
        });
        return subject;
    }

    public void WriteHighScoreRecord(SongMeta songMeta)
    {
        // Not implemented yet.
        return;
    }
}