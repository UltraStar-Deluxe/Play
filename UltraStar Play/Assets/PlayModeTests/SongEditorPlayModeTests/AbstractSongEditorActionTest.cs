using System.Collections.Generic;
using Responsible;
using UniInject;
using UnityEngine;

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

    protected async Awaitable ExpectCurrentSongEqualsExpectedResult(string expectedResultSong)
    {
        await ConditionTestUtils.WaitForConditionAsync(() =>
        {
            UltraStarSongMeta expectedSongMeta = UltraStarSongParser.ParseFile(GetAbsoluteTestSongFilePath(expectedResultSong), out List<SongIssue> _);
            SongMetaAssertUtils.AssertSongMetasAreEqual(expectedSongMeta, SongMeta);
        });
    }
}
