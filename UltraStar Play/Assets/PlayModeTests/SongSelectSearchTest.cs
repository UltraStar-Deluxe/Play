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
        await ExpectAnySongSelectEntryAsync();
        await SetSearchTextAsync("eLLo");
        await ExpectSongSelectEntryWithArtistNameAsync("HèllóArtist");
    }


    [UnityTest]
    public IEnumerator CancelSongSearchShouldGoBackToLastSelection() => CancelSongSearchShouldGoBackToLastSelectionAsync();
    private async Awaitable CancelSongSearchShouldGoBackToLastSelectionAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();
        await ExpectAnySongSelectEntryAsync();
        await SelectSongSelectEntryWithTitleAsync("ArtistHelloWithAccent");
        await WaitForSecondsAsync(1);
        await SetSearchTextAsync("Default");
        await WaitForSecondsAsync(1);
        await CancelSearchAsync();
        await WaitForSecondsAsync(1);
        await ExpectSelectedSongSelectEntryWithTitleAsync("ArtistHelloWithAccent");
    }

    [UnityTest]
    public IEnumerator SubmitSongSearchShouldContinueAtCurrentSelection() => SubmitSongSearchShouldContinueAtCurrentSelectionAsync();
    private async Awaitable SubmitSongSearchShouldContinueAtCurrentSelectionAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();
        await ExpectAnySongSelectEntryAsync();
        await SelectSongSelectEntryWithTitleAsync("ArtistHelloWithAccent");
        await WaitForSecondsAsync(1);
        await SetSearchTextAsync("ArtistHelloNoAccent");
        await WaitForSecondsAsync(1);
        await SubmitSearchAsync();
        await WaitForSecondsAsync(1);
        await ExpectSelectedSongSelectEntryWithTitleAsync("ArtistHelloNoAccent");
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

    private async Awaitable SetSearchTextAsync(string text)
    {
        TextField textField = await GetElementAsync<TextField>(R.UxmlNames.searchTextField);
        await SetElementValueAsync(textField, text);
    }

    private async Awaitable ExpectAnySongSelectEntryAsync()
    {
        await WaitForConditionAsync(() =>
        {
            List<SongSelectSongEntry> songSelectSongEntries = songRouletteControl
                .Entries
                .OfType<SongSelectSongEntry>()
                .ToList();
            return !songSelectSongEntries.IsNullOrEmpty();
        }, new WaitForConditionConfig { description = "expect any song select entry"});
    }

    private async Awaitable ExpectSongSelectEntryWithArtistNameAsync(string text)
    {
        await WaitForConditionAsync(() =>
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

    private async Awaitable ExpectSelectedSongSelectEntryWithTitleAsync(string title)
    {
        await WaitForConditionAsync(() =>
        {
            SongSelectSongEntry songEntry = songRouletteControl.SelectedEntry as SongSelectSongEntry;
            return songEntry.SongMeta.Title == title;
        });
    }

    private async Awaitable SelectSongSelectEntryWithTitleAsync(string title)
    {
        SongSelectEntry matchingSongEntry = songRouletteControl.Entries.FirstOrDefault(entry =>
            entry is SongSelectSongEntry songEntry && songEntry.SongMeta.Title == title);
        songRouletteControl.SelectEntry(matchingSongEntry);
        await WaitForSecondsAsync(0.5f);
    }

    private async Awaitable CancelSearchAsync()
    {
        InputFixture.PressAndRelease(Keyboard.escapeKey);
        await WaitForSecondsAsync(0.5f);
    }

    private async Awaitable SubmitSearchAsync()
    {
        InputFixture.PressAndRelease(Keyboard.enterKey);
        await WaitForSecondsAsync(0.5f);
    }
}
