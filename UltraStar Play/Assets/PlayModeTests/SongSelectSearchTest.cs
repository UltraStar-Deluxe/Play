using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static UnityEngine.Awaitable;
using static ConditionUtils;
using static VisualElementTestUtils;

public class SongSelectSearchTest : AbstractPlayModeTest
{
    protected override string TestSceneName => EScene.SongSelectScene.ToString();

    [Inject]
    private SongSelectSceneControl songSelectSceneControl;

    [Inject]
    private SongRouletteControl songRouletteControl;

    [UnityTest]
    public IEnumerator SongSearchShouldIgnoreAccents() => SongSearchShouldIgnoreAccentsAsync();
    private async Awaitable SongSearchShouldIgnoreAccentsAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();
        await ExpectAnySongSelectEntry();
        await SetSearchText("eLLo");
        await ExpectSongSelectEntryWithArtistName("HèllóArtist");
    }


    [UnityTest]
    public IEnumerator CancelSongSearchShouldGoBackToLastSelection() => CancelSongSearchShouldGoBackToLastSelectionAsync();
    private async Awaitable CancelSongSearchShouldGoBackToLastSelectionAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();
        await ExpectAnySongSelectEntry();
        await SelectSongSelectEntryWithTitle("ArtistHelloWithAccent");
        await WaitForSecondsAsync(1);
        await SetSearchText("Default");
        await WaitForSecondsAsync(1);
        await CancelSearch();
        await WaitForSecondsAsync(1);
        await ExpectSelectedSongSelectEntryWithTitle("ArtistHelloWithAccent");
    }

    [UnityTest]
    public IEnumerator SubmitSongSearchShouldContinueAtCurrentSelection() => SubmitSongSearchShouldContinueAtCurrentSelectionAsync();
    private async Awaitable SubmitSongSearchShouldContinueAtCurrentSelectionAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();
        await ExpectAnySongSelectEntry();
        await SelectSongSelectEntryWithTitle("ArtistHelloWithAccent");
        await WaitForSecondsAsync(1);
        await SetSearchText("ArtistHelloNoAccent");
        await WaitForSecondsAsync(1);
        await SubmitSearch();
        await WaitForSecondsAsync(1);
        await ExpectSelectedSongSelectEntryWithTitle("ArtistHelloNoAccent");
    }

    protected override List<string> GetRelativeTestSongFilePaths()
    {
        return new List<string>
        {
            "SongSearchTestSongs/Default.txt",
            "SongSearchTestSongs/ArtistHelloNoAccent.txt",
            "SongSearchTestSongs/ArtistHelloWithAccent.txt",
        };
    }

    private async Awaitable SetSearchText(string text)
    {
        TextField textField = await GetElement<TextField>(R.UxmlNames.searchTextField);
        await SetElementValue(textField, text);
    }

    private async Awaitable ExpectAnySongSelectEntry()
    {
        await WaitForCondition(() =>
        {
            List<SongSelectSongEntry> songSelectSongEntries = songRouletteControl
                .Entries
                .OfType<SongSelectSongEntry>()
                .ToList();
            return !songSelectSongEntries.IsNullOrEmpty();
        }, new WaitForConditionConfig { description = "expect any song select entry"});
    }

    private async Awaitable ExpectSongSelectEntryWithArtistName(string text)
    {
        await WaitForCondition(() =>
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
        }, new WaitForConditionConfig { description = $"expect song select entry with artist name '{text}'"});
    }

    private async Awaitable ExpectSelectedSongSelectEntryWithTitle(string title)
    {
        await WaitForCondition(() =>
        {
            SongSelectSongEntry songEntry = songRouletteControl.SelectedEntry as SongSelectSongEntry;
            return songEntry.SongMeta.Title == title;
        });
    }

    private async Awaitable SelectSongSelectEntryWithTitle(string title)
    {
        SongSelectEntry matchingSongEntry = songRouletteControl.Entries.FirstOrDefault(entry =>
            entry is SongSelectSongEntry songEntry && songEntry.SongMeta.Title == title);
        songRouletteControl.SelectEntry(matchingSongEntry);
        await WaitForSecondsAsync(0.5f);
    }

    private async Awaitable CancelSearch()
    {
        InputFixture.PressAndRelease(Keyboard.escapeKey);
        await WaitForSecondsAsync(0.5f);
    }

    private async Awaitable SubmitSearch()
    {
        InputFixture.PressAndRelease(Keyboard.enterKey);
        await WaitForSecondsAsync(0.5f);
    }
}
