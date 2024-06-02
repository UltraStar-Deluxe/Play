using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Responsible;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static Responsible.Responsibly;
using static ResponsibleVisualElementUtils;
using static ResponsibleSceneUtils;
using static ResponsibleUtils;

public class LyricsEditingTest : AbstractPlayModeTest
{
    private const string OriginalNoteText = "C5";
    private const string EditedNoteText = "REPLACEMENT";

    protected override string TestSceneName => EScene.SongEditorScene.ToString();

    protected override List<string> GetRelativeTestSongFilePaths()
        => new List<string> { "SingingTestSongs/ThreeQuartersA4OneQuarterC5-TestSong.txt" };

    protected SongEditorSelectionControl SongEditorSelectionControl => GameObject.FindObjectOfType<SongEditorSelectionControl>();
    protected SongMeta SongMeta => SceneNavigator.GetSceneDataOrThrow<SongEditorSceneData>().SongMeta;

    [UnityTest]
    public IEnumerator ShouldEditLyricsOfSingleNote() => ExpectScene(EScene.SongEditorScene)
        .ContinueWith(_ => WaitForThenDoAndReturn("note with original text", () => GetNoteElementByLyrics(OriginalNoteText)))
        .ContinueWith(_ => Do("select next note", () => InputFixture.PressAndRelease(Keyboard.tabKey)))
        .ContinueWith(_ => Do("select next note", () =>  InputFixture.PressAndRelease(Keyboard.tabKey)))
        .ContinueWith(_ => WaitForCondition("expect note selected",
                () => SongEditorSelectionControl.GetSelectedNotes().Count == 1
                      && SongEditorSelectionControl.GetSelectedNotes()[0].Text == OriginalNoteText)
            .ExpectWithinSeconds(10))
        .ContinueWith(_ => Do("open lyrics editing", () => InputFixture.PressAndRelease(Keyboard.f2Key)))
        .ContinueWith(_ => WaitForSeconds(1))
        .ContinueWith(_ => SetElementValue(R.UxmlNames.editLyricsPopupTextField, EditedNoteText))
        .ContinueWith(_ => Do("submit lyrics editing", () => InputFixture.PressAndRelease(Keyboard.enterKey)))
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
