using System;
using System.Collections;
using System.Collections.Generic;
using Responsible;
using UniInject;
using UnityEngine;
using static ResponsibleSceneUtils;
using static Responsible.Responsibly;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public abstract class AbstractSongEditorActionTest : AbstractSongEditorTest
{
    protected override string TestSceneName => "CommonTestScene";

    [Inject(SearchMethod = SearchMethods.FindObjectOfType)]
    protected SongAudioPlayer songAudioPlayer;

    [Inject]
    protected SongEditorSceneData songEditorSceneData;

    protected SongMeta SongMeta => songEditorSceneData.SongMeta;

    protected ITestInstruction<object> ExpectCurrentSongEqualsExpectedResult(string expectedResultSong)
        => DoAndReturn("load expected result",
                () => UltraStarSongParser.ParseFile(GetAbsoluteTestSongFilePath(expectedResultSong),
                    out List<SongIssue> _))
            .ContinueWith(expectedSongMeta =>
                Do("expect current song equals expected result",
                    () => SongMetaAssertUtils.AssertSongMetasAreEqual(expectedSongMeta, SongMeta)));
}
