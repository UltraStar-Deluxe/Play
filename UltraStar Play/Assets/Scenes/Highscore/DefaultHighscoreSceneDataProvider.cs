using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

public class DefaultHighscoreSceneDataProvider : MonoBehaviour, IDefaultSceneDataProvider
{
    public EDifficulty difficulty;

    public SceneData GetDefaultSceneData()
    {
        SongMetaManager.Instance.ScanFilesIfNotDoneYet();
        SongMetaManager.Instance.WaitUntilSongScanFinished();

        HighscoreSceneData highscoreSceneData = new HighscoreSceneData();
        highscoreSceneData.SongMeta = SongMetaManager.Instance.GetFirstSongMeta();
        highscoreSceneData.Difficulty = difficulty;
        return highscoreSceneData;
    }
}
