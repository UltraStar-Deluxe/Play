using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Responsible;
using UniInject;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static ResponsibleSceneUtils;
using static ResponsibleVisualElementUtils;
using static ResponsibleLogAssertUtils;
using static Responsible.Responsibly;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongQueueTest : AbstractPlayModeTest
{
    protected override string TestSceneName => EScene.SongSelectScene.ToString();

    private static string medleySongTitle_0 = "O Christmas Tree";
    private static string medleySongTitle_1 = "ArtistHelloNoAccent";
    private static string medleySongTitle_2 = "ArtistHelloNoAccent";

    [Inject]
    private SongSelectSceneControl songSelectSceneControl;

    [Inject]
    private SongRouletteControl songRouletteControl;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private SongQueueManager songQueueManager;

    protected override List<string> GetRelativeTestSongFilePaths() => new()
    {
        "MedleyTestSongs/OChristmasTree-MedleyStart-MedleyEnd.txt",
        "SongSearchTestSongs/ArtistHelloNoAccent.txt",
        "SongSearchTestSongs/Default.txt",
    };

    [UnityTest]
    public IEnumerator ShouldStartSongsFromQueue() => IgnoreFailingMessages()
        .ContinueWith(ExpectScene(EScene.SongSelectScene))
        .ContinueWith(EnqueueSong(medleySongTitle_0))
        .ContinueWith(WaitForSeconds(0.5f))
        .ContinueWith(EnqueueSong(medleySongTitle_1))
        .ContinueWith(WaitForSeconds(0.5f))
        .ContinueWith(EnqueueSongAsMedley(medleySongTitle_2))
        .ContinueWith(ExpectSongQueue(0, medleySongTitle_0))
        .ContinueWith(ExpectSongQueue(1, medleySongTitle_1, medleySongTitle_2))
        .ToYieldInstruction(Executor);

    private ITestInstruction<object> ExpectSongQueue(int songQueueEntryIndex, params string[] titles) =>
        WaitForCondition($"wait for song queue entry {songQueueEntryIndex} to have titles '{titles.JoinWith(",")}'", () =>
            {
                List<SongQueueEntryDto> nextSongQueueEntries = songQueueManager.GetSongQueueEntries(songQueueEntryIndex);

                string expectedTitlesCsv = titles.JoinWith(",");
                string titlesCsv = nextSongQueueEntries.Select(entry => entry.SongDto.Title).JoinWith(",");
                return string.Equals(titlesCsv, expectedTitlesCsv);
            })
            .ExpectWithinSeconds(10f);

    private ITestInstruction<object> SelectSong(string title) =>
        Do($"select song '{title}'", () =>
            {
                songRouletteControl.SelectEntryBySongMeta(songMetaManager.GetSongMetaByTitle(title));
            })
            .ContinueWith(WaitForSeconds(2f));

    private ITestInstruction<object> EnqueueSong(string title) =>
        SelectSong(title)
            .ContinueWith(ClickSelectedSongMenuButton("enqueueButton"));

    private ITestInstruction<object> EnqueueSongAsMedley(string title) =>
        SelectSong(title)
            .ContinueWith(ClickSelectedSongMenuButton("enqueueAsMedleyButton"));

    private ITestInstruction<object> ClickSelectedSongMenuButton(string uxmlName) =>
        // Cannot use ClickButton method because this button is not focusable
        Do($"click button '{R.UxmlNames.openSongMenuButton}'",
                () => songRouletteControl.SelectedEntryControl.VisualElement.Q<Button>(R.UxmlNames.openSongMenuButton).Click())
            .ContinueWith(WaitForSeconds(0.5f))
            .ContinueWith(ClickButton(uxmlName))
            .ContinueWith(WaitForSeconds(0.5f));
}
