using UnityEngine;

public class DefaultHighscoreSceneDataProvider : MonoBehaviour, IDefaultSceneDataProvider
{
    public EDifficulty difficulty;

    public SceneData GetDefaultSceneData()
    {
        SongMetaManager.Instance.ScanFilesIfNotDoneYet();
        SongMetaManager.Instance.WaitUntilSongScanFinished();

        HighscoreSceneData highscoreSceneData = new();
        highscoreSceneData.SongMeta = SongMetaManager.Instance.GetFirstSongMeta();
        highscoreSceneData.Difficulty = difficulty;
        return highscoreSceneData;
    }
}
