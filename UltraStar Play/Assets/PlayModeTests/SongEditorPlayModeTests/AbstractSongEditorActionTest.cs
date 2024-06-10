using System;
using System.Collections;
using System.Collections.Generic;
using Responsible;
using UnityEngine;
using static ResponsibleSceneUtils;
using static Responsible.Responsibly;

public abstract class AbstractSongEditorActionTest : AbstractSongEditorTest
{
    protected override string TestSceneName => "CommonTestScene";

    protected SongAudioPlayer SongAudioPlayer => GameObject.FindObjectOfType<SongAudioPlayer>();
    protected SongMeta SongMeta => SceneNavigator.GetSceneDataOrThrow<SongEditorSceneData>().SongMeta;

    protected ITestInstruction<object> ExpectCurrentSongEqualsExpectedResult(string expectedResultSong)
        => DoAndReturn("load expected result",
                () => UltraStarSongParser.ParseFile(GetAbsoluteTestSongFilePath(expectedResultSong),
                    out List<SongIssue> _))
            .ContinueWith(expectedSongMeta =>
                Do("expect current song equals expected result",
                    () => SongMetaAssertUtils.AssertSongMetasAreEqual(expectedSongMeta, SongMeta)));
}
