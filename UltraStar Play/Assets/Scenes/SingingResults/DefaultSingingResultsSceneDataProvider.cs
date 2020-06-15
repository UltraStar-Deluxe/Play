using System.Linq;
using UnityEngine;

public class DefaultSingingResultsSceneDataProvider : MonoBehaviour, IDefaultSceneDataProvider
{
    public SceneData GetDefaultSceneData()
    {
        SingingResultsSceneData data = new SingingResultsSceneData();

        SongMetaManager.Instance.WaitUntilSongScanFinished();
        data.SongMeta = SongMetaManager.Instance.GetFirstSongMeta();
        data.SongDurationInMillis = 120 * 1000;

        PlayerScoreControllerData playerScoreData = new PlayerScoreControllerData();
        playerScoreData.TotalScore = 6500;
        playerScoreData.NormalNotesTotalScore = 4000;
        playerScoreData.GoldenNotesTotalScore = 2000;
        playerScoreData.PerfectSentenceBonusTotalScore = 500;

        playerScoreData.NormalNoteLengthTotal = 80;
        playerScoreData.GoldenNoteLengthTotal = 20;
        playerScoreData.PerfectSentenceCount = 10;

        playerScoreData.NormalBeatData.GoodBeats = 30;
        playerScoreData.NormalBeatData.PerfectBeats = 10;
        playerScoreData.GoldenBeatData.GoodBeats = 5;
        playerScoreData.GoldenBeatData.PerfectBeats = 5;

        Sentence sentence1 = new Sentence(0, 200);
        Sentence sentence2 = new Sentence(201, 500);
        Sentence sentence3 = new Sentence(501, 1500);

        playerScoreData.SentenceToSentenceScoreMap.Add(sentence1, CreateSentenceScore(sentence1, 3000));
        playerScoreData.SentenceToSentenceScoreMap.Add(sentence2, CreateSentenceScore(sentence2, 5000));
        playerScoreData.SentenceToSentenceScoreMap.Add(sentence3, CreateSentenceScore(sentence3, 6500));

        PlayerProfile playerProfile = SettingsManager.Instance.Settings.PlayerProfiles[0];
        data.AddPlayerScores(playerProfile, playerScoreData);
        data.PlayerProfileToMicProfileMap[playerProfile] = SettingsManager.Instance.Settings.MicProfiles.FirstOrDefault();
        return data;
    }

    private SentenceScore CreateSentenceScore(Sentence sentence, int totalScoreSoFar)
    {
        SentenceScore sentenceScore = new SentenceScore(sentence);
        sentenceScore.TotalScoreSoFar = totalScoreSoFar;
        return sentenceScore;
    }
}
