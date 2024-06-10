using System.Collections;
using System.Linq;
using Responsible;
using UnityEngine;
using UnityEngine.TestTools;
using static Responsible.Responsibly;
using static ResponsibleSceneUtils;

public class CopyAndPasteTest : AbstractSongEditorActionTest
{
    protected SongEditorSelectionControl SongEditorSelectionControl => Object.FindObjectOfType<SongEditorSelectionControl>();
    protected SongEditorCopyPasteManager SongEditorCopyPasteManager => Object.FindObjectOfType<SongEditorCopyPasteManager>();

    [UnityTest]
    public IEnumerator ShouldCopyAndPasteNotes()
    {
        LogAssert.ignoreFailingMessages = true;

        return OpenSongEditorWithNewSong("SongEditorTestSongs/CopyNotes-Simple.txt")
            .ContinueWith(_ => ExpectScene(EScene.SongEditorScene))
            .ContinueWith(_ => CopyAndPasteNotes())
            .ContinueWith(_ => WaitForSeconds(10))
            .ContinueWith(_ =>
                ExpectCurrentSongEqualsExpectedResult("SongEditorTestSongs/CopyNotes-Simple-Expected.txt"))
            .ToYieldInstruction(this.Executor);
    }

    private ITestInstruction<object> CopyAndPasteNotes()
        => SelectAll()
            .ContinueWith(_ => WaitForSeconds(1))
            .ContinueWith(CopyNotes())
            .ContinueWith(_ => WaitForSeconds(1))
            .ContinueWith(MoveBehindLastNote())
            .ContinueWith(_ => WaitForSeconds(1))
            .ContinueWith(PasteNotes());

    private ITestInstruction<object> MoveBehindLastNote()
        => Do("move behind last note", () => SongAudioPlayer.PositionInMillis = GetPositionBehindLastNoteInMillis());

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
}
