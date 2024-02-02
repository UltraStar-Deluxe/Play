using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Responsible;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static Responsible.Responsibly;
using static ResponsibleVisualElementUtils;
using static ResponsibleFindComponentUtils;

public class SongSelectSearchTests : AbstractPlayModeTest
{
    protected override string TestSceneName => EScene.SongSelectScene.ToString();

    [UnityTest]
    public IEnumerator SongSearchIgnoresAccentsTest() => SetSearchText("mana")
        .ContinueWith(ExpectSongSelectEntryWithArtistName("Maná"))
        .ToYieldInstruction(this.Executor);

    private static ITestInstruction<object> SetSearchText(string text)
        => GetElement<TextField>(R.UxmlNames.searchTextField)
            .ContinueWith(textField => SetElementValue(textField, text));

    private static ITestInstruction<object> ExpectSongSelectEntryWithArtistName(string text) =>
        FindFirstObjectByType<SongSelectSceneControl>()
            .ContinueWith(songSelectSceneControl => WaitForCondition(
                    $"expect song select entry with artist name '{text}'",
                    () =>
                    {
                        List<SongSelectSongEntry> songSelectSongEntries = songSelectSceneControl
                            .songRouletteControl
                            .Entries
                            .OfType<SongSelectSongEntry>()
                            .ToList();
                        return songSelectSongEntries.Count == 1
                               && songSelectSongEntries.FirstOrDefault()
                                   .SongMeta
                                   .Artist
                                   .Contains(text, StringComparison.InvariantCultureIgnoreCase);
                    }).ExpectWithinSeconds(1));
}
