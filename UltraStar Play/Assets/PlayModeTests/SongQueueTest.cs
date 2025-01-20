using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static UnityEngine.Awaitable;
using static ConditionTestUtils;
using static SceneConditionTestUtils;
using static VisualElementTestUtils;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongQueueTest : AbstractPlayModeTest
{
    protected override string TestSceneName => EScene.SongSelectScene.ToString();

    private static string medleySongTitle_0 = "O Christmas Tree";
    private static string medleySongTitle_1 = "ArtistHelloNoAccent";
    private static string medleySongTitle_2 = "Default";

    [Inject]
    private SongSelectSceneControl songSelectSceneControl;

    [Inject]
    private SongRouletteControl songRouletteControl;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private SongQueueManager songQueueManager;

    [Inject]
    private UIDocument uiDocument;

    protected override List<string> GetRelativeTestSongFilePaths() => new()
    {
        "MedleyTestSongs/OChristmasTree-MedleyStart-MedleyEnd.txt",
        "SongSearchTestSongs/ArtistHelloNoAccent.txt",
        "SongSearchTestSongs/Default.txt",
    };

    [UnityTest]
    public IEnumerator ShouldStartSongsFromQueue() => ShouldStartSongsFromQueueAsync();
    private async Awaitable ShouldStartSongsFromQueueAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();

        // When
        await ExpectScene(EScene.SongSelectScene);
        await EnqueueSong(medleySongTitle_0);
        await WaitForSecondsAsync(0.2f);
        await EnqueueSong(medleySongTitle_1);
        await WaitForSecondsAsync(0.2f);
        await EnqueueSongAsMedley(medleySongTitle_2);

        // Then
        await ExpectSongQueue(0, medleySongTitle_0);
        await ExpectSongQueue(1, medleySongTitle_1, medleySongTitle_2);

        // TODO: The test execution terminates without proper error message when attempting to change to SingScene.
        // await StartSingingWithSongQueue();
        // await WaitUntilScene(EScene.SingScene);
        // await ExpectSongQueue(0, medleySongTitle_1, medleySongTitle_2);
    }

    private async Awaitable ExpectSongQueue(int songQueueEntryIndex, params string[] titles)
    {
        await WaitForCondition(() =>
        {
            List<SongQueueEntryDto> nextSongQueueEntries =
                songQueueManager.GetSongQueueEntries(songQueueEntryIndex);

            string expectedTitlesCsv = titles.JoinWith(",");
            string titlesCsv = nextSongQueueEntries.Select(entry => entry.SongDto.Title).JoinWith(",");
            return string.Equals(titlesCsv, expectedTitlesCsv);
        }, new WaitForConditionConfig { description = $"wait for song queue entry {songQueueEntryIndex} to have titles '{titles.JoinWith(",")}'"});
    }

    private async Awaitable EnqueueSong(string title)
    {
        songRouletteControl.SelectEntryBySongMeta(songMetaManager.GetSongMetaByTitle(title));
        await WaitForSecondsAsync(1);
        await ClickSelectedSongMenuButton("enqueueButton");
    }

    private async Awaitable EnqueueSongAsMedley(string title)
    {
        songRouletteControl.SelectEntryBySongMeta(songMetaManager.GetSongMetaByTitle(title));
        await WaitForSecondsAsync(1f);
        await ClickSelectedSongMenuButton("enqueueAsMedleyButton");
    }

    private async Awaitable ClickSelectedSongMenuButton(string uxmlName)
    {
        // Cannot use ClickButton method because this button is not focusable
        songRouletteControl.SelectedEntryControl.VisualElement.Q<Button>(R.UxmlNames.openSongMenuButton)
            .SendClickEvent();
        await WaitForSecondsAsync(0.2f);
        await ClickButton(uxmlName);
        await WaitForSecondsAsync(0.2f);
    }

    private async Awaitable StartSingingWithSongQueue()
    {
        await ClickButton(R.UxmlNames.toggleSongQueueOverlayButton);
        await WaitForSecondsAsync(0.5f);
        await ClickButton(R.UxmlNames.startSongQueueButton);
    }
}
