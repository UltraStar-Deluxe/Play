using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static ConditionUtils;
using Assert = UnityEngine.Assertions.Assert;

public class LyricsEditingTest : AbstractPlayModeTest
{
    private const string OriginalNoteText = "C5";
    private const string EditedNoteText = "REPLACEMENT";

    protected override string TestSceneName => EScene.SongEditorScene.ToString();

    protected override List<string> GetRelativeTestSongFilePaths()
        => new List<string> { "SingingTestSongs/ThreeQuartersA4OneQuarterC5.txt" };

    [Inject(SearchMethod = SearchMethods.FindObjectOfType)]
    private SongEditorSelectionControl songEditorSelectionControl;

    [Inject]
    private SongEditorSceneData songEditorSceneData;
    private SongMeta SongMeta => songEditorSceneData.SongMeta;

    [UnityTest]
    [Ignore("Flaky test when started via 'Run All'")] // TODO: Fix flaky test
    public IEnumerator ShouldEditLyricsOfSingleNote() => ShouldEditLyricsOfSingleNoteAsync();
    private async Awaitable ShouldEditLyricsOfSingleNoteAsync()
    {
        await SceneConditionTestUtils.ExpectSceneAsync(EScene.SongEditorScene);
        await SelectNextNoteAsync();
        await SelectNextNoteAsync();
        await ExpectSelectedNoteAsync(OriginalNoteText);

        await OpenLyricsPopupEditorAsync();
        await VisualElementTestUtils.SetElementValueAsync(R.UxmlNames.editLyricsPopupTextField, EditedNoteText);
        await SubmitLyricsPopupEditorAsync();
        await WaitForConditionAsync(
            () => SongMetaUtils.GetLyrics(SongMeta, EVoiceId.P1).Contains(EditedNoteText),
            new WaitForConditionConfig { description = $"expect lyrics to contain '{EditedNoteText}'"});
    }

    [UnityTest]
    public IEnumerator ShouldEditLyricsViaLyricsArea() => ShouldEditLyricsViaLyricsAreaAsync();
    private async Awaitable ShouldEditLyricsViaLyricsAreaAsync()
    {
        await SceneConditionTestUtils.ExpectSceneAsync(EScene.SongEditorScene);

        // Given: Original lyrics
        TextField textField = await VisualElementTestUtils.GetElementAsync<TextField>(R.UxmlNames.lyricsAreaTextField);
        Assert.IsTrue(textField.value.Contains(OriginalNoteText));

        // When: Edit LyricsArea
        await VisualElementTestUtils.ClickButtonAsync(R.UxmlNames.toggleLyricsAreaEditModeButton);
        await Awaitable.WaitForSecondsAsync(1f);
        await VisualElementTestUtils.SetElementValueAsync(textField, textField.value.Replace(OriginalNoteText, EditedNoteText));
        await Awaitable.WaitForSecondsAsync(1f);

        // Then: LyricsArea has changed text
        Assert.IsTrue(textField.value.Contains(EditedNoteText));

        // When: Submit LyricsArea
        await VisualElementTestUtils.ClickButtonAsync(R.UxmlNames.toggleLyricsAreaEditModeButton);
        await Awaitable.WaitForSecondsAsync(1f);

        // Then: Song has changed text
        Assert.IsTrue(SongMetaUtils.GetLyrics(SongMeta, EVoiceId.P1).Contains(EditedNoteText));
    }

    private async Awaitable SubmitLyricsPopupEditorAsync()
    {
        InputFixture.PressAndRelease(Keyboard.enterKey);
        await Awaitable.WaitForSecondsAsync(0.1f);
    }

    private async Awaitable OpenLyricsPopupEditorAsync()
    {
        InputFixture.PressAndRelease(Keyboard.f2Key);
        await Awaitable.WaitForSecondsAsync(0.1f);
    }

    private async Awaitable ExpectSelectedNoteAsync(string lyrics)
    {
        await WaitForConditionAsync(() =>
            {
                List<Note> selectedNotes = songEditorSelectionControl.GetSelectedNotes();
                return selectedNotes.Count == 1 && selectedNotes[0].Text == lyrics;
            },
            new WaitForConditionConfig { description = $"expect selected note with lyrics '{lyrics}'"});
    }

    private async Awaitable SelectNextNoteAsync()
    {
        InputFixture.PressAndRelease(Keyboard.tabKey);
        await Awaitable.WaitForSecondsAsync(0.1f);
    }
}
