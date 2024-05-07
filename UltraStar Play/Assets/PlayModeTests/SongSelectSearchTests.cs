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
    public IEnumerator SongSearchShouldIgnoreAccents() => ExpectAnySongSelectEntry()
        .ContinueWith(_ => SetSearchText("eLLo"))
        .ContinueWith(ExpectSongSelectEntryWithArtistName("HèllóArtist"))
        .ToYieldInstruction(this.Executor);

    protected override List<string> GetRelativeTestSongFilePaths()
    {
        return new List<string>
        {
            "SongSearchTestSongs/Default-TestSong.txt",
            "SongSearchTestSongs/ArtistHelloNoAccent-TestSong.txt",
            "SongSearchTestSongs/ArtistHelloWithAccent-TestSong.txt",
        };
    }

    private static ITestInstruction<object> SetSearchText(string text)
        => GetElement<TextField>(R.UxmlNames.searchTextField)
            .ContinueWith(textField => SetElementValue(textField, text));


    private static ITestInstruction<object> ExpectAnySongSelectEntry() =>
        FindFirstObjectByType<SongSelectSceneControl>()
            .ContinueWith(songSelectSceneControl => WaitForCondition(
                $"expect any song select entry",
                () =>
                {
                    List<SongSelectSongEntry> songSelectSongEntries = songSelectSceneControl
                        .songRouletteControl
                        .Entries
                        .OfType<SongSelectSongEntry>()
                        .ToList();
                    return !songSelectSongEntries.IsNullOrEmpty();
                }).ExpectWithinSeconds(1));

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

                        // Expect less songs than before, but expect the one with the given artist.
                        return songSelectSongEntries.Count == 2
                               && songSelectSongEntries.AnyMatch(songSelectSongEntry => songSelectSongEntry
                                   .SongMeta
                                   .Artist
                                   .Contains(text, StringComparison.InvariantCultureIgnoreCase));
                    }).ExpectWithinSeconds(5));
}
