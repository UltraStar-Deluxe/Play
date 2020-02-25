using UnityEngine;

public class DefaultSingingResultsSceneDataProvider : MonoBehaviour, IDefaultSceneDataProvider
{
    public SceneData GetDefaultSceneData()
    {
        SingingResultsSceneData data = new SingingResultsSceneData();

        SongMetaManager.Instance.WaitUntilSongScanFinished();
        data.SongMeta = SongMetaManager.Instance.GetFirstSongMeta();

        SingingResultsSceneData.PlayerScoreData playerScoreData = new SingingResultsSceneData.PlayerScoreData();
        playerScoreData.TotalScore = 6500;
        playerScoreData.NormalNotesScore = 4000;
        playerScoreData.GoldenNotesScore = 2000;
        playerScoreData.PerfectSentenceBonusScore = 500;

        PlayerProfile playerProfile = SettingsManager.Instance.Settings.PlayerProfiles[0];
        data.AddPlayerScores(playerProfile, playerScoreData);
        return data;
    }
}
