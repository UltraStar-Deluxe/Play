using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public class SongListTest : AbstractConnectedCompanionAppPlayModeTest
{
    private static readonly List<TestCaseData> filterSongListTestCases = new List<TestCaseData>()
    {
        // Filter by artist
        new TestCaseData("3 doors down").Returns(null),

        // Filter by title
        new TestCaseData("Kryptonite").Returns(null),

        // Filter case insensitive
        new TestCaseData("KRYPTOnite").Returns(null),

        // Ignore diacritics
        new TestCaseData("Kryptònîté").Returns(null),
    };

    [Inject(UxmlName = R.UxmlNames.showSongViewButton)]
    private Button showSongViewButton;

    [Inject(UxmlName = R.UxmlNames.songListView)]
    private ListView songListView;

    [Inject(UxmlName = R.UxmlNames.songSearchTextField)]
    private TextField songSearchTextField;

    [Inject]
    private SongListPageObject songListPageObject;

    [UnityTest]
    [TestCaseSource("filterSongListTestCases")]
    public IEnumerator ShouldFilterSongList(string searchText) => ShouldFilterSongListAsync(searchText);
    private async Awaitable ShouldFilterSongListAsync(string searchText)
    {
        LogAssertUtils.IgnoreFailingMessages();

        // When
        await songListPageObject.OpenAsync();
        await ConditionUtils.WaitForConditionAsync(() => songListView.itemsSource.Count > 1,
            new WaitForConditionConfig { description = "song list has multiple entries" });
        songListPageObject.SetSearchText(searchText);

        // Then
        await ConditionUtils.WaitForConditionAsync(() => songListView.itemsSource.Count == 1,
            new WaitForConditionConfig { description = $"song list has single entry matching '{searchText}'" });
    }
}
