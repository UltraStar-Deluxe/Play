using System.Collections;
using System.Collections.Generic;
using Responsible;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static Responsible.Responsibly;
using static ResponsibleSceneUtils;
using static ResponsibleVisualElementUtils;

public class SongSelectPlaylistTests : AbstractPlayModeTest
{
    private const string TestPlaylistName = "TestPlaylist";

    protected override string TestSceneName => EScene.SongSelectScene.ToString();

    protected override List<string> GetRelativeTestSongFilePaths()
    {
        return new List<string>
        {
            "SongSearchTestSongs/Default-TestSong.txt",
            "SongSearchTestSongs/ArtistHelloNoAccent-TestSong.txt",
            "SongSearchTestSongs/ArtistHelloWithAccent-TestSong.txt",
        };
    }

    private PlaylistManager PlaylistManager => PlaylistManager.Instance;
    private SongRouletteControl SongRouletteControl => GameObject.FindObjectOfType<SongRouletteControl>();
    private SettingsManager SettingsManager => GameObject.FindObjectOfType<SettingsManager>();

    [UnityTest]
    public IEnumerator PlaylistShouldWork()
    {
        try
        {
            yield return ExpectScene(EScene.SongSelectScene)
                .ContinueWith(_ => WaitForCondition("all song visible",
                            () => SongRouletteControl.SongEntries.Count == 3)
                        .ExpectWithinSeconds(1))
                .ContinueWith(_ => WaitForCondition("no test playlist exists",
                        () => !PlaylistManager.HasPlaylist(TestPlaylistName))
                    .ExpectWithinSeconds(1))
                .ContinueWith(_ => ClickButton(R.UxmlNames.searchPropertyButton))
                .ContinueWith(_ => ClickButton(R.UxmlNames.createPlaylistButton))
                .ContinueWith(_ => SetElementValue("newPlaylistNameTextField", TestPlaylistName))
                .ContinueWith(_ => ClickButton(R.Messages.common_ok))
                .ContinueWith(_ => WaitForCondition("test playlist is empty",
                        () => PlaylistManager.GetPlaylistByName(TestPlaylistName) != null
                              && PlaylistManager.GetPlaylistByName(TestPlaylistName).IsEmpty)
                        .ExpectWithinSeconds(1))
                .ContinueWith(_ => Do("open song entry menu", () => InputFixture.PressAndRelease(Keyboard.spaceKey)))
                .ContinueWith(_ => GetElement<Button>(button => button.Query<Label>().ToList()
                    .AnyMatch(label => label.text.Contains(TestPlaylistName))))
                .ContinueWith(button => ClickButton(button))
                .ContinueWith(_ => WaitForCondition("entry added to playlist", () => PlaylistManager.GetPlaylistByName(TestPlaylistName).Count == 1).ExpectWithinSeconds(1))
                // .ContinueWith(_ => ClickButton(R.UxmlNames.searchPropertyButton))
                .ContinueWith(_ => SetElementValue(R.UxmlNames.playlistDropdownField, TestPlaylistName))
                .ContinueWith(_ => WaitForCondition("test playlist selected",
                            () => SettingsManager.NonPersistentSettings.PlaylistName.Value == TestPlaylistName)
                            .ExpectWithinSeconds(1))
                .ContinueWith(_ => WaitForCondition("filtered song visible",
                            () => SongRouletteControl.SongEntries.Count == 1)
                        .ExpectWithinSeconds(1))
                .ContinueWith(_ => ClickButton(R.UxmlNames.editPlaylistButton))
                .ContinueWith(_ => ClickButton(R.UxmlNames.deletePlaylistButton))
                .ContinueWith(_ => ClickButton(R.UxmlNames.confirmDeletePlaylistButton))
                .ContinueWith(_ => WaitForCondition("no test playlist exists",
                        () => !PlaylistManager.HasPlaylist(TestPlaylistName))
                    .ExpectWithinSeconds(1))
                .ToYieldInstruction(this.Executor);
        }
        finally
        {
            PlaylistManager.TryRemovePlaylist(PlaylistManager.GetPlaylistByName(TestPlaylistName));
        }
    }
}
