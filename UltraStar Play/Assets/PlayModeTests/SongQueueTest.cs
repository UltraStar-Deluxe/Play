using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

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
    public IEnumerator ShouldStartSongsFromQueue()
    {
        LogAssertUtils.IgnoreFailingMessages();
        yield return WaitUntilScene(EScene.SongSelectScene);
        yield return EnqueueSong(medleySongTitle_0);
        yield return new WaitForSeconds(0.2f);
        yield return EnqueueSong(medleySongTitle_1);
        yield return new WaitForSeconds(0.2f);
        yield return EnqueueSongAsMedley(medleySongTitle_2);
        yield return ExpectSongQueue(0, medleySongTitle_0);
        yield return ExpectSongQueue(1, medleySongTitle_1, medleySongTitle_2);

        // TODO: The test execution terminates without proper error message when attempting to change to SingScene.
        // yield return StartSingingWithSongQueue();
        // yield return WaitUntilScene(EScene.SingScene);
        // yield return ExpectSongQueue(0, medleySongTitle_1, medleySongTitle_2);
    }

    private CustomYieldInstruction WaitUntilScene(EScene scene)
    {
        return new WaitUntilWithTimeout($"wait for scene {scene}", TimeSpan.FromMilliseconds(1000),
            () => SceneNavigator.Instance.CurrentScene == scene);
    }

    private IEnumerator ExpectSongQueue(int songQueueEntryIndex, params string[] titles)
    {
        yield return new WaitUntilWithTimeout(
            $"wait for song queue entry {songQueueEntryIndex} to have titles '{titles.JoinWith(",")}'",
            TimeSpan.FromMilliseconds(1000),
            () =>
            {
                List<SongQueueEntryDto> nextSongQueueEntries =
                    songQueueManager.GetSongQueueEntries(songQueueEntryIndex);

                string expectedTitlesCsv = titles.JoinWith(",");
                string titlesCsv = nextSongQueueEntries.Select(entry => entry.SongDto.Title).JoinWith(",");
                return string.Equals(titlesCsv, expectedTitlesCsv);
            });
    }

    private IEnumerator EnqueueSong(string title)
    {
        songRouletteControl.SelectEntryBySongMeta(songMetaManager.GetSongMetaByTitle(title));
        yield return new WaitForSeconds(1f);
        yield return ClickSelectedSongMenuButton("enqueueButton");
    }

    private IEnumerator EnqueueSongAsMedley(string title)
    {
        songRouletteControl.SelectEntryBySongMeta(songMetaManager.GetSongMetaByTitle(title));
        yield return new WaitForSeconds(1f);
        yield return ClickSelectedSongMenuButton("enqueueAsMedleyButton");
    }

    private IEnumerator ClickSelectedSongMenuButton(string uxmlName)
    {
        // Cannot use ClickButton method because this button is not focusable
        songRouletteControl.SelectedEntryControl.VisualElement.Q<Button>(R.UxmlNames.openSongMenuButton)
            .SendClickEvent();
        yield return new WaitForSeconds(0.2f);
        yield return ClickButton(uxmlName);
        yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator ClickButton(string uxmlName)
    {
        yield return new WaitUntilWithTimeout($"wait until button can be clicked: {uxmlName}", TimeSpan.FromSeconds(10),
            () => VisualElementUtils.IsFocusableNow(uiDocument.rootVisualElement.Q<Button>(uxmlName), uiDocument));
        uiDocument.rootVisualElement.Q<Button>(uxmlName).SendClickEvent();
    }

    private IEnumerator StartSingingWithSongQueue()
    {
        yield return ClickButton(R.UxmlNames.toggleSongQueueOverlayButton);
        yield return new WaitForSeconds(0.5f);
        yield return ClickButton(R.UxmlNames.startSongQueueButton);
    }
}
