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
        .ContinueWith(WaitForThenDoAndReturn("note with original text", () => GetNoteElementByLyrics(OriginalNoteText)))
        .ContinueWith(Do("select next note", () => InputFixture.PressAndRelease(Keyboard.tabKey)))
        .ContinueWith(Do("select next note", () =>  InputFixture.PressAndRelease(Keyboard.tabKey)))
        .ContinueWith(WaitForCondition("expect note selected",
                () => songEditorSelectionControl.GetSelectedNotes().Count == 1
                      && songEditorSelectionControl.GetSelectedNotes()[0].Text == OriginalNoteText)
            .ExpectWithinSeconds(10))
        .ContinueWith(Do("open lyrics editing", () => InputFixture.PressAndRelease(Keyboard.f2Key)))
        .ContinueWith(WaitForSeconds(1))
        .ContinueWith(SetElementValue(R.UxmlNames.editLyricsPopupTextField, EditedNoteText))
        .ContinueWith(Do("submit lyrics editing", () => InputFixture.PressAndRelease(Keyboard.enterKey)))
        .ContinueWith(WaitForCondition("expect lyrics have been changed",
            () => SongMetaUtils.GetLyrics(SongMeta, EVoiceId.P1).Contains(EditedNoteText))
            .ExpectWithinSeconds(10))
        .ToYieldInstruction(Executor);

    [UnityTest]
    public IEnumerator ShouldEditLyricsViaLyricsArea() => IgnoreFailingMessages()
        .ContinueWith(ExpectScene(EScene.SongEditorScene))
        .ContinueWith(WaitForThenDoAndReturn("note with original text", () => GetNoteElementByLyrics(OriginalNoteText)))
        .ContinueWith(ClickButton(R.UxmlNames.toggleLyricsAreaEditModeButton))
        .ContinueWith(WaitForSeconds(1))
        .ContinueWith(GetElement<TextField>(R.UxmlNames.lyricsAreaTextField))
        .ContinueWith(textField => WaitForCondition("TextField has original text",
                () => textField.value.Contains(OriginalNoteText)).ExpectWithinSeconds(10))
        .ContinueWith(GetElement<TextField>(R.UxmlNames.lyricsAreaTextField))
        .ContinueWith(textField => SetElementValue(textField, textField.value.Replace(OriginalNoteText, EditedNoteText)))
        .ContinueWith(GetElement<TextField>(R.UxmlNames.lyricsAreaTextField))
        .ContinueWith(textField => WaitForCondition("TextField has edited text",
            () => textField.value.Contains(EditedNoteText)).ExpectWithinSeconds(10))
        .ContinueWith(WaitForSeconds(1))
        .ContinueWith(ClickButton(R.UxmlNames.toggleLyricsAreaEditModeButton))
        .ContinueWith(WaitForCondition("expect lyrics have been changed",
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
