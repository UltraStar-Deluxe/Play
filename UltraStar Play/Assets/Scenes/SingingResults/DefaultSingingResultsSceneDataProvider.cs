using System.Linq;
using UnityEngine;

public class DefaultSingingResultsSceneDataProvider : MonoBehaviour, IDefaultSceneDataProvider
{
    [Range(1, 16)]
    public int playerCount = 1;

    public SceneData GetDefaultSceneData()
    {
        SingingResultsSceneData data = new();

        SongMetaManager.Instance.WaitUntilSongScanFinished();
        data.SongMeta = SongMetaManager.Instance.GetFirstSongMeta();
        data.SongDurationInMillis = 120 * 1000;

        PlayerScoreControlData playerScoreData = new();
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

        Sentence sentence1 = CreateDummySentence(0, 200);
        Sentence sentence2 = CreateDummySentence(201, 500);
        Sentence sentence3 = CreateDummySentence(501, 1500);

        playerScoreData.SentenceToSentenceScoreMap.Add(sentence1, CreateSentenceScore(sentence1, 3000));
        playerScoreData.SentenceToSentenceScoreMap.Add(sentence2, CreateSentenceScore(sentence2, 5000));
        playerScoreData.SentenceToSentenceScoreMap.Add(sentence3, CreateSentenceScore(sentence3, 6500));

        PlayerProfile playerProfile = SettingsManager.Instance.Settings.PlayerProfiles[0];
        data.PlayerProfileToMicProfileMap[playerProfile] = SettingsManager.Instance.Settings.MicProfiles.FirstOrDefault();
        data.AddPlayerScores(playerProfile, playerScoreData);
        for (int i = 1; i < playerCount; i++)
        {
            data.AddPlayerScores(SettingsManager.Instance.Settings.PlayerProfiles[i], playerScoreData);
        }
        return data;
    }

    private Sentence CreateDummySentence(int startBeat, int endBeat)
    {
        int noteCount = 3;
        int noteLength = 10;
        Sentence sentence = new(startBeat, endBeat);
        for (int i = 0; i < noteCount; i++)
        {
            Note note = new(ENoteType.Normal, startBeat + (noteLength * i), noteLength, 0, "b");
            sentence.AddNote(note);
        }
        return sentence;
    }

    private SentenceScore CreateSentenceScore(Sentence sentence, int totalScoreSoFar)
    {
        SentenceScore sentenceScore = new(sentence);
        sentenceScore.TotalScoreSoFar = totalScoreSoFar;
        return sentenceScore;
    }
}
