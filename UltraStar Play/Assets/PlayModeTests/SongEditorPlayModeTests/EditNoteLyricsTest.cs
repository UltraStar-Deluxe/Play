using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

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
    public IEnumerator ShouldEditLyricsOfSingleNote() => ShouldEditLyricsOfSingleNoteAsync();
    private async Awaitable ShouldEditLyricsOfSingleNoteAsync()
    {
        await SceneConditionTestUtils.ExpectScene(EScene.SongEditorScene);
        await SelectNextNote();
        await SelectNextNote();
        await ExpectSelectedNote(OriginalNoteText);

        await OpenLyricsPopupEditor();
        await VisualElementTestUtils.SetElementValue(R.UxmlNames.editLyricsPopupTextField, EditedNoteText);
        await SubmitLyricsPopupEditor();
        await ConditionTestUtils.WaitForCondition(
            () => SongMetaUtils.GetLyrics(SongMeta, EVoiceId.P1).Contains(EditedNoteText),
            new WaitForConditionConfig { description = $"expect lyrics to contain '{EditedNoteText}'"});
    }

    [UnityTest]
    public IEnumerator ShouldEditLyricsViaLyricsArea() => ShouldEditLyricsViaLyricsAreaAsync();
    private async Awaitable ShouldEditLyricsViaLyricsAreaAsync()
    {
        await SceneConditionTestUtils.ExpectScene(EScene.SongEditorScene);

        // Given: Original lyrics
        TextField textField = await VisualElementTestUtils.GetElement<TextField>(R.UxmlNames.lyricsAreaTextField);
        Assert.IsTrue(textField.value.Contains(OriginalNoteText));

        // When: Edit LyricsArea
        await VisualElementTestUtils.ClickButton(R.UxmlNames.toggleLyricsAreaEditModeButton);
        await Awaitable.WaitForSecondsAsync(1f);
        await VisualElementTestUtils.SetElementValue(textField, textField.value.Replace(OriginalNoteText, EditedNoteText));
        await Awaitable.WaitForSecondsAsync(1f);

        // Then: LyricsArea has changed text
        Assert.IsTrue(textField.value.Contains(EditedNoteText));

        // When: Submit LyricsArea
        await VisualElementTestUtils.ClickButton(R.UxmlNames.toggleLyricsAreaEditModeButton);
        await Awaitable.WaitForSecondsAsync(1f);

        // Then: Song has changed text
        Assert.IsTrue(SongMetaUtils.GetLyrics(SongMeta, EVoiceId.P1).Contains(EditedNoteText));
    }

    private async Awaitable SubmitLyricsPopupEditor()
    {
        InputFixture.PressAndRelease(Keyboard.enterKey);
        await Awaitable.WaitForSecondsAsync(0.1f);
    }

    private async Awaitable OpenLyricsPopupEditor()
    {
        InputFixture.PressAndRelease(Keyboard.f2Key);
        await Awaitable.WaitForSecondsAsync(0.1f);
    }

    private async Awaitable ExpectSelectedNote(string lyrics)
    {
        await ConditionTestUtils.WaitForCondition(() =>
            {
                List<Note> selectedNotes = songEditorSelectionControl.GetSelectedNotes();
                return selectedNotes.Count == 1 && selectedNotes[0].Text == lyrics;
            },
            new WaitForConditionConfig { description = $"expect selected note with lyrics '{lyrics}'"});
    }

    private async Awaitable SelectNextNote()
    {
        InputFixture.PressAndRelease(Keyboard.tabKey);
        await Awaitable.WaitForSecondsAsync(0.1f);
    }
}
