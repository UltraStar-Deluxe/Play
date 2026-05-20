using System.Collections.Generic;
using UniInject;
using UnityEngine;
using static ConditionUtils;

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

    protected async Awaitable ExpectCurrentSongEqualsExpectedResultAsync(string expectedResultSong)
    {
        await WaitForConditionAsync(() =>
        {
            UltraStarSongMeta expectedSongMeta = UltraStarSongParser.ParseFile(GetAbsoluteTestSongFilePath(expectedResultSong)).SongMeta;
            SongMetaAssertUtils.AssertSongMetasAreEqual(expectedSongMeta, SongMeta);
        });
    }
}
