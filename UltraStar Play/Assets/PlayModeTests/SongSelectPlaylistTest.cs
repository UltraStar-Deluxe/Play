using System.Collections;
using System.Collections.Generic;
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

public class SongSelectPlaylistTest : AbstractPlayModeTest
{
    private const string TestPlaylistName = "TestPlaylist";

    protected override string TestSceneName => EScene.SongSelectScene.ToString();

    protected override List<string> GetRelativeTestSongFilePaths()
    {
        return new List<string>
        {
            "SongSearchTestSongs/Default.txt",
            "SongSearchTestSongs/ArtistHelloNoAccent.txt",
            "SongSearchTestSongs/ArtistHelloWithAccent.txt",
        };
    }

    [Inject]
    private PlaylistManager playlistManager;

    [Inject]
    private NonPersistentSettings nonPersistentSettings;

    [Inject(SearchMethod = SearchMethods.FindObjectOfType)]
    private SongRouletteControl songRouletteControl;

    [UnityTest]
    public IEnumerator PlaylistShouldWork() => PlaylistShouldWorkAsync();
    private async Awaitable PlaylistShouldWorkAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();

        try
        {
            // Given: Start without playlist
            await ExpectScene(EScene.SongSelectScene);
            await WaitForCondition(() => songRouletteControl.SongEntries.Count == 3,
                new WaitForConditionConfig {description = "all song visible"} );
            playlistManager.TryRemovePlaylist(playlistManager.GetPlaylistByName(TestPlaylistName));
            await WaitForCondition(() => !playlistManager.HasPlaylist(TestPlaylistName),
                    new WaitForConditionConfig { description = "no test playlist exists"});

            // When: Create playlist
            await ClickButton(R.UxmlNames.searchPropertyButton);
            await NextFrameAsync();
            await ClickButton(R.UxmlNames.createPlaylistButton);
            await NextFrameAsync();
            await SetElementValue("newPlaylistNameTextField", TestPlaylistName);
            await ClickButton(R.Messages.common_ok);
            await NextFrameAsync();

            // Then: Playlist created and is empty
            await WaitForCondition(
                () => playlistManager.GetPlaylistByName(TestPlaylistName)?.IsEmpty ?? false,
                new WaitForConditionConfig { description = "test playlist is empty" });

            // When: Add song to playlist
            await OpenSongEntryMenu();
            await NextFrameAsync();
            Button button = await GetElement<Button>(button => button.Query<Label>().ToList()
                .AnyMatch(label => label.text.Contains(TestPlaylistName)));
            await ClickButton(button);

            // Then: Playlist contains song
            await WaitForCondition(
                () => playlistManager.GetPlaylistByName(TestPlaylistName).Count == 1,
                new WaitForConditionConfig { description = "entry added to playlist" });

            // When: Select playlist
            await ClickButton(R.UxmlNames.searchPropertyButton);
            await SetElementValue(R.UxmlNames.playlistDropdownField, TestPlaylistName);

            // Then: Playlist selected and songs filtered by playlist
            await WaitForCondition(() => nonPersistentSettings.PlaylistName.Value == TestPlaylistName,
                new WaitForConditionConfig { description = "test playlist selected"});
            await WaitForCondition(() => songRouletteControl.SongEntries.Count == 1,
                new WaitForConditionConfig { description = "filtered song visible" });

            // When: Delete playlist
            await ClickButton(R.UxmlNames.editPlaylistButton);
            await ClickButton(R.UxmlNames.deletePlaylistButton);
            await ClickButton(R.UxmlNames.confirmDeletePlaylistButton);

            // Then: Playlist does not exist
            await WaitForCondition(() => !playlistManager.HasPlaylist(TestPlaylistName),
                new WaitForConditionConfig { description = "no test playlist exists" });
        }
        finally
        {
            playlistManager.TryRemovePlaylist(playlistManager.GetPlaylistByName(TestPlaylistName));
        }
    }

    private async Awaitable OpenSongEntryMenu()
    {
        InputFixture.PressAndRelease(Keyboard.spaceKey);
        await WaitForSecondsAsync(0.1f);
    }
}
