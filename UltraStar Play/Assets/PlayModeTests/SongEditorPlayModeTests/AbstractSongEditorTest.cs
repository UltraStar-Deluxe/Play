using System;
using System.Collections.Generic;
using Responsible;
using static Responsible.Responsibly;

public abstract class AbstractSongEditorTest : AbstractPlayModeTest
{
    protected SceneNavigator SceneNavigator => SceneNavigator.Instance;
    protected SongMetaManager SongMetaManager => SongMetaManager.Instance;

    protected override string TestSceneName => EScene.SongEditorScene.ToString();

    protected override List<string> GetRelativeTestSongFilePaths()
        => new List<string> { "SongEditorTestSongs/EditSongMeta.txt" };

    protected ITestInstruction<object> OpenSongEditorWithNewSong(string fileName)
        => DoAndReturn($"add song '{fileName}'", () =>
        {
            string filePath = GetAbsoluteTestSongFilePath(fileName);
            SongMeta songMeta = UltraStarSongParser.ParseFile(filePath, out List<SongIssue> _);
            SongMeta existingSongMeta = SongMetaManager.GetSongMetaByTitle(songMeta.Title);
            if (existingSongMeta != null)
            {
                throw new Exception($"Song with title {songMeta.Title} already exists");
            }
            SongMetaManager.AddSongMeta(songMeta);
            return songMeta;
        }).ContinueWith(songMeta => Do($"load song editor with '{songMeta.GetArtistDashTitle()}'",
            () => SceneNavigator.LoadScene(EScene.SongEditorScene, new SongEditorSceneData()
        {
            SongMeta = songMeta,
            PreviousScene = EScene.SongSelectScene,
        })));
}
