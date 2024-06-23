using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Responsible;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static Responsible.Responsibly;
using static ResponsibleVisualElementUtils;
using static ResponsibleSceneUtils;
using static ResponsibleUtils;
using static ResponsibleLogAssertUtils;

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
    public IEnumerator ShouldEditLyricsOfSingleNote() => IgnoreFailingMessages()
        .ContinueWith(ExpectScene(EScene.SongEditorScene))
        .ContinueWith(_ => WaitForThenDoAndReturn("note with original text", () => GetNoteElementByLyrics(OriginalNoteText)))
        .ContinueWith(_ => Do("select next note", () => InputFixture.PressAndRelease(Keyboard.tabKey)))
        .ContinueWith(_ => Do("select next note", () =>  InputFixture.PressAndRelease(Keyboard.tabKey)))
        .ContinueWith(_ => WaitForCondition("expect note selected",
                () => songEditorSelectionControl.GetSelectedNotes().Count == 1
                      && songEditorSelectionControl.GetSelectedNotes()[0].Text == OriginalNoteText)
            .ExpectWithinSeconds(10))
        .ContinueWith(_ => Do("open lyrics editing", () => InputFixture.PressAndRelease(Keyboard.f2Key)))
        .ContinueWith(_ => WaitForSeconds(1))
        .ContinueWith(_ => SetElementValue(R.UxmlNames.editLyricsPopupTextField, EditedNoteText))
        .ContinueWith(_ => Do("submit lyrics editing", () => InputFixture.PressAndRelease(Keyboard.enterKey)))
        .ContinueWith(_ => WaitForCondition("expect lyrics have been changed",
            () => SongMetaUtils.GetLyrics(SongMeta, EVoiceId.P1).Contains(EditedNoteText))
            .ExpectWithinSeconds(10))
        .ToYieldInstruction(Executor);

    [UnityTest]
    public IEnumerator ShouldEditLyricsViaLyricsArea() => IgnoreFailingMessages()
        .ContinueWith(ExpectScene(EScene.SongEditorScene))
        .ContinueWith(_ => WaitForThenDoAndReturn("note with original text", () => GetNoteElementByLyrics(OriginalNoteText)))
        .ContinueWith(_ => ClickButton(R.UxmlNames.toggleLyricsAreaEditModeButton))
        .ContinueWith(_ => WaitForSeconds(1))
        .ContinueWith(_ => GetElement<TextField>(R.UxmlNames.lyricsAreaTextField))
        .ContinueWith(textField => WaitForCondition("TextField has original text",
                () => textField.value.Contains(OriginalNoteText)).ExpectWithinSeconds(10))
        .ContinueWith(_ => GetElement<TextField>(R.UxmlNames.lyricsAreaTextField))
        .ContinueWith(textField => SetElementValue(textField, textField.value.Replace(OriginalNoteText, EditedNoteText)))
        .ContinueWith(_ => GetElement<TextField>(R.UxmlNames.lyricsAreaTextField))
        .ContinueWith(textField => WaitForCondition("TextField has edited text",
            () => textField.value.Contains(EditedNoteText)).ExpectWithinSeconds(10))
        .ContinueWith(_ => WaitForSeconds(1))
        .ContinueWith(_ => ClickButton(R.UxmlNames.toggleLyricsAreaEditModeButton))
        .ContinueWith(_ => WaitForCondition("expect lyrics have been changed",
                () => SongMetaUtils.GetLyrics(SongMeta, EVoiceId.P1).Contains(EditedNoteText))
            .ExpectWithinSeconds(10))
        .ToYieldInstruction(Executor);

    private VisualElement GetNoteElementByLyrics(string lyrics)
    {
        return UIDocumentUtils.FindUIDocumentOrThrow().rootVisualElement
            .Query<VisualElement>(R.UxmlNames.noteUiRoot)
            .ToList()
            .SelectMany(noteUiRoot => noteUiRoot.Query<Label>().ToList())
            .FirstOrDefault(label => label.text == lyrics);
    }
}
