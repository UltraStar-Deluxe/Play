using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;
using static UnityEngine.Awaitable;
using static ConditionUtils;

public class SongSelectNavigationByOrderPropertyTest : AbstractPlayModeTest
{
    private const float FuzzySearchResetTimeInSeconds = 2f;

    protected override string TestSceneName => EScene.SongSelectScene.ToString();

    [Inject]
    private SongRouletteControl songRouletteControl;

    [Inject]
    private SongSearchControl songSearchControl;

    [Inject]
    private SongSelectSceneControl songSelectSceneControl;

    protected override List<string> GetRelativeTestSongFilePaths()
    {
        return new List<string>
        {
            "SongNavigationByOrderPropertyTestSongs/AArtist - ATitle.txt",
            "SongNavigationByOrderPropertyTestSongs/AArtist - BTitle.txt",
            "SongNavigationByOrderPropertyTestSongs/BArtist - ATitle.txt",
            "SongNavigationByOrderPropertyTestSongs/BArtist - BTitle.txt",
            "SongNavigationByOrderPropertyTestSongs/CArtist - ATitle.txt",
            "SongNavigationByOrderPropertyTestSongs/CArtist - BTitle.txt",
        };
    }

    [UnityTest]
    public IEnumerator ShouldNavigateByOrderProperty() => ShouldNavigateByOrderPropertyAsync();
    private async Awaitable ShouldNavigateByOrderPropertyAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();

        // Given
        await ExpectSelectedSong("AArtist", "ATitle");

        // When: Navigate right
        // TODO: InputFixture does not work
        // InputFixture.Press(Keyboard.leftShiftKey);
        // InputFixture.PressAndRelease(Keyboard.rightArrowKey);
        // InputFixture.Release(Keyboard.leftShiftKey);
        songSearchControl.SelectNextEntryByOrderProperty();
        await WaitForSecondsAsync(0.5f);
        // Then
        await ExpectSelectedSong("BArtist", "ATitle");

        // When: Navigate right
        // InputFixture.Press(Keyboard.leftShiftKey);
        // InputFixture.PressAndRelease(Keyboard.rightArrowKey);
        // InputFixture.Release(Keyboard.leftShiftKey);
        songSearchControl.SelectNextEntryByOrderProperty();
        await WaitForSecondsAsync(0.5f);
        // Then
        await ExpectSelectedSong("CArtist", "ATitle");

        // When: Navigate left
        // InputFixture.Press(Keyboard.leftShiftKey);
        // InputFixture.PressAndRelease(Keyboard.leftArrowKey);
        // InputFixture.Release(Keyboard.leftShiftKey);
        songSearchControl.SelectPreviousEntryByOrderProperty();
        await WaitForSecondsAsync(0.5f);
        // Then
        await ExpectSelectedSong("BArtist", "BTitle");
    }

    [UnityTest]
    public IEnumerator ShouldNavigateByOrderPropertyOnLetterKey() => ShouldNavigateByOrderPropertyOnLetterKeyAsync();
    private async Awaitable ShouldNavigateByOrderPropertyOnLetterKeyAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();
        // Given
        await ExpectSelectedSong("AArtist", "ATitle");

        // When: type key
        // InputFixture.PressAndRelease(Keyboard.bKey);
        songSelectSceneControl.DoFuzzySearch("b");
        await WaitForSecondsAsync(FuzzySearchResetTimeInSeconds);
        // Then
        await ExpectSelectedSong("BArtist", "ATitle");

        // When: type key
        // InputFixture.PressAndRelease(Keyboard.cKey);
        songSelectSceneControl.DoFuzzySearch("c");
        await WaitForSecondsAsync(FuzzySearchResetTimeInSeconds);
        // Then
        await ExpectSelectedSong("CArtist", "ATitle");

        // When: type key
        // InputFixture.PressAndRelease(Keyboard.aKey);
        songSelectSceneControl.DoFuzzySearch("a");
        await WaitForSecondsAsync(FuzzySearchResetTimeInSeconds);
        // Then
        await ExpectSelectedSong("AArtist", "ATitle");
    }

    private async Awaitable ExpectSelectedSong(string artist, string title)
    {
        await WaitForConditionAsync(() => songRouletteControl.SelectedEntry is SongSelectSongEntry songEntry
                                          && songEntry.SongMeta.Artist == artist
                                          && songEntry.SongMeta.Title == title,
            new WaitForConditionConfig { description = $"expect selected song '{artist} - {title}'" });
    }
}
