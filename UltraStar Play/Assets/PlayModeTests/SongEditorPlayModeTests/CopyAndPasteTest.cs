using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Responsible;
using UnityEngine;
using UnityEngine.TestTools;
using static Responsible.Responsibly;
using static ResponsibleSceneUtils;
using static ResponsibleLogAssertUtils;

public class CopyAndPasteTest : AbstractSongEditorActionTest
{
    private static readonly List<TestCaseData> testCases = new List<TestCaseData>()
    {
        new TestCaseData("SongEditorTestSongs/Copy-Note.txt").Returns(null),
        new TestCaseData("SongEditorTestSongs/Copy-Sentence.txt").Returns(null),
        new TestCaseData("SongEditorTestSongs/Copy-Sentences.txt").Returns(null),
        new TestCaseData("SongEditorTestSongs/Copy-Voices.txt").Returns(null),
    };

    protected SongEditorSelectionControl SongEditorSelectionControl => Object.FindObjectOfType<SongEditorSelectionControl>();
    protected SongEditorCopyPasteManager SongEditorCopyPasteManager => Object.FindObjectOfType<SongEditorCopyPasteManager>();
    protected SongEditorSceneInputControl SongEditorSceneInputControl => Object.FindObjectOfType<SongEditorSceneInputControl>();

    [UnityTest]
    [TestCaseSource(nameof(testCases))]
    public IEnumerator CopyAndPasteShouldPreserveNotes(string songFilePath) => IgnoreFailingMessages()
        .ContinueWith(OpenSongEditorWithNewSong(songFilePath))
        .ContinueWith(ExpectScene(EScene.SongEditorScene))
        // Select all and copy
        .ContinueWith(SelectAll())
        .ContinueWith(WaitForSeconds(1))
        .ContinueWith(CopyNotes())
        .ContinueWith(WaitForSeconds(1))
        // Select all again, go to first note, then delete
        .ContinueWith(SelectAll())
        .ContinueWith(WaitForSeconds(1))
        .ContinueWith(MoveToFirstSelectedNote())
        .ContinueWith(WaitForSeconds(1))
        .ContinueWith(DeleteNotes())
        .ContinueWith(WaitForSeconds(1))
        // Paste
        .ContinueWith(PasteNotes())
        .ContinueWith(WaitForSeconds(1))

        .ContinueWith(ExpectCurrentSongEqualsExpectedResult(songFilePath))
        .ToYieldInstruction(this.Executor);

    private ITestInstruction<object> MoveToFirstSelectedNote()
        => Do("move to first selected note", () => SongAudioPlayer.PositionInMillis = GetFirstSelectedNotePositionInMillis());

    private ITestInstruction<object> MoveBehindLastNote()
        => Do("move behind last note", () => SongAudioPlayer.PositionInMillis = GetPositionBehindLastNoteInMillis());

    private ITestInstruction<object> DeleteNotes()
        => Do("delete selected notes", () => SongEditorSceneInputControl.DeleteSelectedNotes());

    private ITestInstruction<object> CopyNotes()
        // TODO: Input simulation does not work reliably for some reason
        // => TriggerInputAction(R.InputActions.songEditor_copy);
        => Do("copy selected notes", () => SongEditorCopyPasteManager.CopySelectedNotes());

    private ITestInstruction<object> PasteNotes()
        // TODO: Input simulation does not work reliably for some reason
        // => TriggerInputAction(R.InputActions.songEditor_paste);
        => Do("paste copied notes", () => SongEditorCopyPasteManager.PasteCopiedNotes());

    private ITestInstruction<object> SelectAll()
        // TODO: Input simulation does not work reliably for some reason
        // => Do("select all", () => TriggerInputAction(R.InputActions.songEditor_selectAll))
        => Do("select all", () => SongEditorSelectionControl.SelectAll())
            .ContinueWith(_ => WaitForCondition("has selected notes", () => !SongEditorSelectionControl.GetSelectedNotes().IsNullOrEmpty())
                .ExpectWithinSeconds(10));

    private double GetPositionBehindLastNoteInMillis()
    {
        int positionInBeats = SongMetaUtils.GetAllNotes(SongMeta).Select(note => note.EndBeat).Max() + 2;
        return SongMetaBpmUtils.BeatsToMillis(SongMeta, positionInBeats);
    }

    private double GetFirstSelectedNotePositionInMillis()
    {
        int positionInBeats = SongEditorSelectionControl.GetSelectedNotes().Select(note => note.StartBeat).Min();
        return SongMetaBpmUtils.BeatsToMillis(SongMeta, positionInBeats);
    }
}
