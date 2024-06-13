using System.Collections;
using System.Collections.Generic;
using Responsible;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static Responsible.Responsibly;
using static ResponsibleSceneUtils;
using static ResponsibleVisualElementUtils;

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
    public IEnumerator PlaylistShouldWork()
    {
        try
        {
            yield return ExpectScene(EScene.SongSelectScene)
                .ContinueWith(_ => WaitForCondition("all song visible",
                            () => songRouletteControl.SongEntries.Count == 3)
                        .ExpectWithinSeconds(10))
                .ContinueWith(_ => WaitForCondition("no test playlist exists",
                        () => !playlistManager.HasPlaylist(TestPlaylistName))
                    .ExpectWithinSeconds(10))
                .ContinueWith(_ => ClickButton(R.UxmlNames.searchPropertyButton))
                .ContinueWith(_ => ClickButton(R.UxmlNames.createPlaylistButton))
                .ContinueWith(_ => SetElementValue("newPlaylistNameTextField", TestPlaylistName))
                .ContinueWith(_ => ClickButton(R.Messages.common_ok))
                .ContinueWith(_ => WaitForCondition("test playlist is empty",
                        () => playlistManager.GetPlaylistByName(TestPlaylistName) != null
                              && playlistManager.GetPlaylistByName(TestPlaylistName).IsEmpty)
                        .ExpectWithinSeconds(10))
                .ContinueWith(_ => Do("open song entry menu", () => InputFixture.PressAndRelease(Keyboard.spaceKey)))
                .ContinueWith(_ => GetElement<Button>(button => button.Query<Label>().ToList()
                    .AnyMatch(label => label.text.Contains(TestPlaylistName))))
                .ContinueWith(button => ClickButton(button))
                .ContinueWith(_ => WaitForCondition("entry added to playlist", () => playlistManager.GetPlaylistByName(TestPlaylistName).Count == 1).ExpectWithinSeconds(10))
                // .ContinueWith(_ => ClickButton(R.UxmlNames.searchPropertyButton))
                .ContinueWith(_ => SetElementValue(R.UxmlNames.playlistDropdownField, TestPlaylistName))
                .ContinueWith(_ => WaitForCondition("test playlist selected",
                            () => nonPersistentSettings.PlaylistName.Value == TestPlaylistName)
                            .ExpectWithinSeconds(10))
                .ContinueWith(_ => WaitForCondition("filtered song visible",
                            () => songRouletteControl.SongEntries.Count == 1)
                        .ExpectWithinSeconds(10))
                .ContinueWith(_ => ClickButton(R.UxmlNames.editPlaylistButton))
                .ContinueWith(_ => ClickButton(R.UxmlNames.deletePlaylistButton))
                .ContinueWith(_ => ClickButton(R.UxmlNames.confirmDeletePlaylistButton))
                .ContinueWith(_ => WaitForCondition("no test playlist exists",
                        () => !playlistManager.HasPlaylist(TestPlaylistName))
                    .ExpectWithinSeconds(10))
                .ToYieldInstruction(this.Executor);
        }
        finally
        {
            playlistManager.TryRemovePlaylist(playlistManager.GetPlaylistByName(TestPlaylistName));
        }
    }
}
