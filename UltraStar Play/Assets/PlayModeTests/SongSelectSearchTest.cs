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

public class SongSelectSearchTest : AbstractPlayModeTest
{
    protected override string TestSceneName => EScene.SongSelectScene.ToString();

    [UnityTest]
    public IEnumerator SongSearchShouldIgnoreAccents() => ExpectAnySongSelectEntry()
        .ContinueWith(SetSearchText("eLLo"))
        .ContinueWith(ExpectSongSelectEntryWithArtistName("HèllóArtist"))
        .ToYieldInstruction(this.Executor);

    [UnityTest]
    public IEnumerator CancelSongSearchShouldGoBackToLastSelection() => ExpectAnySongSelectEntry()
        .ContinueWith(SelectSongSelectEntryWithTitle("ArtistHelloWithAccent"))
        .ContinueWith(WaitForSeconds(1))
        .ContinueWith(SetSearchText("Default"))
        .ContinueWith(WaitForSeconds(1))
        .ContinueWith(CancelSearch())
        .ContinueWith(WaitForSeconds(1))
        .ContinueWith(ExpectSelectedSongSelectEntryWithTitle("ArtistHelloWithAccent"))
        .ToYieldInstruction(this.Executor);

    [UnityTest]
    public IEnumerator SubmitSongSearchShouldContinueAtCurrentSelection() => ExpectAnySongSelectEntry()
        .ContinueWith(SelectSongSelectEntryWithTitle("ArtistHelloWithAccent"))
        .ContinueWith(WaitForSeconds(1))
        .ContinueWith(SetSearchText("ArtistHelloNoAccent"))
        .ContinueWith(WaitForSeconds(1))
        .ContinueWith(SubmitSearch())
        .ContinueWith(WaitForSeconds(1))
        .ContinueWith(ExpectSelectedSongSelectEntryWithTitle("ArtistHelloNoAccent"))
        .ToYieldInstruction(this.Executor);

    protected override List<string> GetRelativeTestSongFilePaths()
    {
        return new List<string>
        {
            "SongSearchTestSongs/Default.txt",
            "SongSearchTestSongs/ArtistHelloNoAccent.txt",
            "SongSearchTestSongs/ArtistHelloWithAccent.txt",
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
                }).ExpectWithinSeconds(10));

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

    private static ITestInstruction<object> ExpectSelectedSongSelectEntryWithTitle(string title) =>
        FindFirstObjectByType<SongSelectSceneControl>()
            .ContinueWith(songSelectSceneControl => WaitForCondition(
                $"expect selected song entry with title '{title}'",
                () => (songSelectSceneControl.songRouletteControl.SelectedEntry as SongSelectSongEntry).SongMeta.Title == title)
                .ExpectWithinSeconds(5));

    private static ITestInstruction<object> SelectSongSelectEntryWithTitle(string title) =>
        FindFirstObjectByType<SongSelectSceneControl>()
            .ContinueWith(songSelectSceneControl => Do(
                    $"select song entry with title '{title}'",
                    () =>
                    {
                        songSelectSceneControl.songRouletteControl.SelectEntry(songSelectSceneControl.songRouletteControl.Entries
                            .FirstOrDefault(entry => entry is SongSelectSongEntry songEntry && songEntry.SongMeta.Title == title));
                    }));

    private ITestInstruction<object> CancelSearch()
        => Do($"cancel search",
            () => InputFixture.PressAndRelease(Keyboard.escapeKey));

    private ITestInstruction<object> SubmitSearch()
        => Do($"submit search",
            () => InputFixture.PressAndRelease(Keyboard.enterKey));
}
