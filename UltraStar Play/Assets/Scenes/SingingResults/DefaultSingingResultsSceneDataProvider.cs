using System.Linq;
using UnityEngine;

public class DefaultSingingResultsSceneDataProvider : MonoBehaviour, IDefaultSceneDataProvider
{
    public SceneData GetDefaultSceneData()
    {
        SingingResultsSceneData data = new SingingResultsSceneData();

        SongMetaManager.Instance.WaitUntilSongScanFinished();
        data.SongMeta = SongMetaManager.Instance.GetFirstSongMeta();

        PlayerScoreControllerData playerScoreData = new PlayerScoreControllerData();
        playerScoreData.TotalScore = 6500;
        playerScoreData.NormalNotesTotalScore = 4000;
        playerScoreData.GoldenNotesTotalScore = 2000;
        playerScoreData.PerfectSentenceBonusTotalScore = 500;

        PlayerProfile playerProfile = SettingsManager.Instance.Settings.PlayerProfiles[0];
        data.AddPlayerScores(playerProfile, playerScoreData);
        data.PlayerProfileToMicProfileMap[playerProfile] = SettingsManager.Instance.Settings.MicProfiles.FirstOrDefault();
        return data;
    }
}
