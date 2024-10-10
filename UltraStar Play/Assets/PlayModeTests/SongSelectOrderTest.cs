using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Responsible;
using UniInject;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static Responsible.Responsibly;
using static ResponsibleVisualElementUtils;
using static ResponsibleLogAssertUtils;

public class SongSelectOrderTest : AbstractPlayModeTest
{
    protected override string TestSceneName => EScene.SongSelectScene.ToString();

    private static readonly List<TestCaseData> testCases = new List<TestCaseData>()
    {
        new TestCaseData(ESongOrder.Artist, new List<string>() { "ArtistA", "ArtistB", "ArtistC", }).Returns(null),
        new TestCaseData(ESongOrder.Title, new List<string>() { "ArtistC", "ArtistB", "ArtistA", }).Returns(null),
    };

    [Inject]
    private SongRouletteControl songRouletteControl;

    protected override List<string> GetRelativeTestSongFilePaths()
    {
        return new List<string>
        {
            "SongOrderTestSongs/ArtistA - TitleC.txt",
            "SongOrderTestSongs/ArtistB - TitleB.txt",
            "SongOrderTestSongs/ArtistC - TitleA.txt",
        };
    }

    [UnityTest]
    [TestCaseSource(nameof(testCases))]
    public IEnumerator OrderShouldAffectSongSelectEntries(ESongOrder songOrder, List<string> expectedArtistOrder) => IgnoreFailingMessages()
        .ContinueWith(ClickButton(R.UxmlNames.searchPropertyButton))
        .ContinueWith(WaitForSeconds(0.5f))
        .ContinueWith(SelectSongOrder(songOrder))
        .ContinueWith(WaitForSeconds(0.5f))
        .ContinueWith(ExpectSongSelectEntriesInOrder(expectedArtistOrder))
        .ToYieldInstruction(this.Executor);

    private ITestInstruction<object> SelectSongOrder(ESongOrder songOrder)
        => GetElement<DropdownField>(R.UxmlNames.songOrderDropdownField)
            .ContinueWith(element => SetElementValue(element, songOrder.ToString()));

    private ITestInstruction<object> ExpectSongSelectEntriesInOrder(List<string> expectedArtistOrder) =>
        WaitForCondition($"expect songs ordered by '{expectedArtistOrder.JoinWith(", ")}'",
                    () =>
                    {
                        return songRouletteControl.SongEntries.Select(entry => entry.SongMeta.Artist).ToList()
                            .SequenceEqual(expectedArtistOrder);
                    }).ExpectWithinSeconds(5);
}
