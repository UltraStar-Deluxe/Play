using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class AbstractSongEditorTest : AbstractPlayModeTest
{
    protected SceneNavigator SceneNavigator => SceneNavigator.Instance;
    protected SongMetaManager SongMetaManager => SongMetaManager.Instance;

    protected override string TestSceneName => EScene.SongEditorScene.ToString();

    protected override List<string> GetRelativeTestSongFilePaths()
        => new List<string> { "SongEditorTestSongs/EditSongMeta.txt" };

    protected async Awaitable OpenSongEditorWithNewSongAsync(string fileName)
    {
        // Add song
        Debug.Log($"add song '{fileName}'");
        string filePath = GetAbsoluteTestSongFilePath(fileName);
        SongMeta songMeta = UltraStarSongParser.ParseFile(filePath).SongMeta;
        SongMeta existingSongMeta = SongMetaManager.GetSongMetaByTitle(songMeta.Title);
        if (existingSongMeta != null)
        {
            throw new Exception($"Song with title {songMeta.Title} already exists");
        }
        SongMetaManager.AddSongMeta(songMeta);
        await Awaitable.NextFrameAsync();

        // Load song editor
        Debug.Log($"load song editor with '{songMeta.GetArtistDashTitle()}'");
        SceneNavigator.LoadScene(EScene.SongEditorScene, new SongEditorSceneData()
        {
            SongMeta = songMeta,
            PreviousScene = EScene.SongSelectScene,
        });
        await SceneConditionTestUtils.ExpectSceneAsync(EScene.SongEditorScene);
        Debug.Log($"loaded song editor successfully with '{songMeta.GetArtistDashTitle()}'");
        await Awaitable.WaitForSecondsAsync(0.1f);
    }
}
