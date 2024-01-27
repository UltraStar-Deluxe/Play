using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Responsible;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static Responsible.Bdd.Keywords;
using static Responsible.Responsibly;

public class SongSelectSearchTests : AbstractPlayModeTest
{
    protected override string TestSceneName => EScene.SongSelectScene.ToString();

    private TextField SearchTextField => UIDocumentUtils.FindUIDocumentOrThrow()
        .rootVisualElement
        .Q<TextField>(R.UxmlNames.searchTextField);

    [UnityTest]
    public IEnumerator SongSearchIgnoresAccentsTest() => this.Executor.YieldScenario(
        Scenario("should ignore accents when searching songs"),
        When("searching song without accent character", SetSearchText("mana")),
        Then("has found song with accent character", AssertSearchResultContainsSongWithAccentCharacter())
    );

    private ITestInstruction<object> SetSearchText(string text) => Do(
        $"write search text '{text}'",
        () => SearchTextField.value = text)
        .ContinueWith(WaitForSeconds(2));

    private ITestInstruction<object> AssertSearchResultContainsSongWithAccentCharacter() => WaitForCondition(
        nameof(AssertSearchResultContainsSongWithAccentCharacter),
        () =>
        {
            SongSelectSceneControl songSelectSceneControl = UltraStarPlaySceneInjectionManager.Instance
                .SceneInjector
                .GetValueForInjectionKey<SongSelectSceneControl>();
            List<SongSelectSongEntry> songSelectSongEntries = songSelectSceneControl
                .songRouletteControl
                .Entries
                .OfType<SongSelectSongEntry>()
                .ToList();
            return songSelectSongEntries.Count == 1
                && songSelectSongEntries.FirstOrDefault().SongMeta.Artist.Contains("maná", StringComparison.InvariantCultureIgnoreCase);
        }).ExpectWithinSeconds(1);
}
