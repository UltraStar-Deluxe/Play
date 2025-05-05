using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static UnityEngine.Awaitable;
using static ConditionUtils;
using static VisualElementTestUtils;

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
    public IEnumerator OrderShouldAffectSongSelectEntries(ESongOrder songOrder, List<string> expectedArtistOrder) =>
        OrderShouldAffectSongSelectEntriesAsync(songOrder, expectedArtistOrder);
    private async Awaitable OrderShouldAffectSongSelectEntriesAsync(ESongOrder songOrder, List<string> expectedArtistOrder)
    {
        LogAssertUtils.IgnoreFailingMessages();
        await ClickButtonAsync(R.UxmlNames.searchPropertyButton);
        await WaitForSecondsAsync(0.5f);
        await SelectSongOrderAsync(songOrder);
        await WaitForSecondsAsync(0.5f);
        await ExpectSongSelectEntriesInOrderAsync(expectedArtistOrder);
    }

    private async Awaitable SelectSongOrderAsync(ESongOrder songOrder)
    {
        DropdownField element = await GetElementAsync<DropdownField>(R.UxmlNames.songOrderDropdownField);
        await SetElementValueAsync(element, songOrder.ToString());
    }

    private async Awaitable ExpectSongSelectEntriesInOrderAsync(List<string> expectedArtistOrder)
    {
        await WaitForConditionAsync(
                () => songRouletteControl.SongEntries.Select(entry => entry.SongMeta.Artist).ToList().SequenceEqual(expectedArtistOrder),
                new WaitForConditionConfig { description = $"expect songs ordered by '{expectedArtistOrder.JoinWith(", ")}'" });
    }
}
