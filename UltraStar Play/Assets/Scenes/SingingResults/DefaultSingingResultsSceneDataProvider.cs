using System.Linq;
using UnityEngine;

public class DefaultSingingResultsSceneDataProvider : MonoBehaviour, IDefaultSceneDataProvider
{
    public SceneData GetDefaultSceneData()
    {
        SingingResultsSceneData data = new SingingResultsSceneData();

        SongMetaManager.Instance.WaitUntilSongScanFinished();
        data.SongMeta = SongMetaManager.Instance.GetFirstSongMeta();

        SingingResultsSceneData.PlayerScoreResultData playerScoreData = new SingingResultsSceneData.PlayerScoreResultData();
        playerScoreData.TotalScore = 6500;
        playerScoreData.NormalNotesScore = 4000;
        playerScoreData.GoldenNotesScore = 2000;
        playerScoreData.PerfectSentenceBonusScore = 500;

        PlayerProfile playerProfile = SettingsManager.Instance.Settings.PlayerProfiles[0];
        data.AddPlayerScores(playerProfile, playerScoreData);
        data.PlayerProfileToMicProfileMap[playerProfile] = SettingsManager.Instance.Settings.MicProfiles.FirstOrDefault();
        return data;
    }
}
