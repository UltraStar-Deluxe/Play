using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
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
    private string TestPlaylistFilePath => $"{ApplicationUtils.PlaylistFolder}/{TestPlaylistName}.{ApplicationUtils.UltraStarPlaylistFileExtension}";

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
                .ContinueWith(WaitForCondition("all song visible",
                            () => songRouletteControl.SongEntries.Count == 3)
                        .ExpectWithinSeconds(10))
                .ContinueWith(Do("remove old test playlist",
                        () => playlistManager.TryRemovePlaylist(playlistManager.GetPlaylistByName(TestPlaylistName))))
                .ContinueWith(WaitForCondition("no test playlist exists",
                        () => !playlistManager.HasPlaylist(TestPlaylistName))
                    .ExpectWithinSeconds(10))
                .ContinueWith(ClickButton(R.UxmlNames.searchPropertyButton))
                .ContinueWith(WaitForFrames(1))
                .ContinueWith(ClickButton(R.UxmlNames.createPlaylistButton))
                .ContinueWith(WaitForFrames(1))
                .ContinueWith(SetElementValue("newPlaylistNameTextField", TestPlaylistName))
                .ContinueWith(ClickButton(R.Messages.common_ok))
                .ContinueWith(WaitForFrames(1))
                .ContinueWith(WaitForCondition("test playlist is empty",
                        () => playlistManager.GetPlaylistByName(TestPlaylistName) != null
                              && playlistManager.GetPlaylistByName(TestPlaylistName).IsEmpty)
                        .ExpectWithinSeconds(10))
                .ContinueWith(Do("open song entry menu", () => InputFixture.PressAndRelease(Keyboard.spaceKey)))
                .ContinueWith(WaitForFrames(1))
                .ContinueWith(GetElement<Button>(button => button.Query<Label>().ToList()
                    .AnyMatch(label => label.text.Contains(TestPlaylistName))))
                .ContinueWith(button => ClickButton(button))
                .ContinueWith(WaitForCondition("entry added to playlist", () => playlistManager.GetPlaylistByName(TestPlaylistName).Count == 1).ExpectWithinSeconds(10))
                // .ContinueWith(ClickButton(R.UxmlNames.searchPropertyButton))
                .ContinueWith(SetElementValue(R.UxmlNames.playlistDropdownField, TestPlaylistName))
                .ContinueWith(WaitForCondition("test playlist selected",
                            () => nonPersistentSettings.PlaylistName.Value == TestPlaylistName)
                            .ExpectWithinSeconds(10))
                .ContinueWith(WaitForCondition("filtered song visible",
                            () => songRouletteControl.SongEntries.Count == 1)
                        .ExpectWithinSeconds(10))
                .ContinueWith(ClickButton(R.UxmlNames.editPlaylistButton))
                .ContinueWith(ClickButton(R.UxmlNames.deletePlaylistButton))
                .ContinueWith(ClickButton(R.UxmlNames.confirmDeletePlaylistButton))
                .ContinueWith(WaitForCondition("no test playlist exists",
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
