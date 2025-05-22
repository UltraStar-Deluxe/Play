using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static UnityEngine.Awaitable;
using static ConditionUtils;
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
    [Ignore("Flaky test when started via 'Run All'")] // TODO: Fix flaky test
    public IEnumerator PlaylistShouldWork() => PlaylistShouldWorkAsync();
    private async Awaitable PlaylistShouldWorkAsync()
    {
        LogAssertUtils.IgnoreFailingMessages();

        try
        {
            // Given: Start without playlist
            await ExpectSceneAsync(EScene.SongSelectScene);
            await WaitForConditionAsync(() => songRouletteControl.SongEntries.Count == 3,
                new WaitForConditionConfig {description = "all song visible"} );
            playlistManager.TryRemovePlaylist(playlistManager.GetPlaylistByName(TestPlaylistName));
            await WaitForConditionAsync(() => !playlistManager.HasPlaylist(TestPlaylistName),
                    new WaitForConditionConfig { description = "no test playlist exists"});

            // When: Create playlist
            await ClickButtonAsync(R.UxmlNames.searchPropertyButton);
            await NextFrameAsync();
            await ClickButtonAsync(R.UxmlNames.createPlaylistButton);
            await NextFrameAsync();
            await SetElementValueAsync("newPlaylistNameTextField", TestPlaylistName);
            await ClickButtonAsync(R.Messages.common_ok);
            await NextFrameAsync();

            // Then: Playlist created and is empty
            await WaitForConditionAsync(
                () => playlistManager.GetPlaylistByName(TestPlaylistName)?.IsEmpty ?? false,
                new WaitForConditionConfig { description = "test playlist is empty" });

            // When: Add song to playlist
            await OpenSongEntryMenuAsync();
            await NextFrameAsync();
            Button button = await GetElementAsync<Button>(button => button.Query<Label>().ToList()
                .AnyMatch(label => label.text.Contains(TestPlaylistName)));
            await ClickButtonAsync(button);

            // Then: Playlist contains song
            await WaitForConditionAsync(
                () => playlistManager.GetPlaylistByName(TestPlaylistName).Count == 1,
                new WaitForConditionConfig { description = "entry added to playlist" });

            // When: Select playlist
            await SetElementValueAsync(R.UxmlNames.playlistDropdownField, TestPlaylistName);

            // Then: Playlist selected and songs filtered by playlist
            await WaitForConditionAsync(() => nonPersistentSettings.PlaylistName.Value == TestPlaylistName,
                new WaitForConditionConfig { description = "test playlist selected"});
            await WaitForConditionAsync(() => songRouletteControl.SongEntries.Count == 1,
                new WaitForConditionConfig { description = "filtered song visible" });

            // When: Delete playlist
            await ClickButtonAsync(R.UxmlNames.editPlaylistButton);
            await ClickButtonAsync(R.UxmlNames.deletePlaylistButton);
            await ClickButtonAsync(R.UxmlNames.confirmDeletePlaylistButton);

            // Then: Playlist does not exist
            await WaitForConditionAsync(() => !playlistManager.HasPlaylist(TestPlaylistName),
                new WaitForConditionConfig { description = "no test playlist exists" });
        }
        finally
        {
            playlistManager.TryRemovePlaylist(playlistManager.GetPlaylistByName(TestPlaylistName));
        }
    }

    private async Awaitable OpenSongEntryMenuAsync()
    {
        InputFixture.PressAndRelease(Keyboard.spaceKey);
        await WaitForSecondsAsync(0.1f);
    }
}
