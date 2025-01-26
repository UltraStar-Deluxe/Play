using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UniInject;
using UnityEngine;
using UnityEngine.TestTools;
using static ConditionUtils;
using static SceneConditionTestUtils;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CopyAndPasteTest : AbstractSongEditorActionTest
{
    private float waitTimeInSeconds = 0.1f;

    private static readonly List<TestCaseData> testCases = new List<TestCaseData>()
    {
        new TestCaseData("SongEditorTestSongs/Copy-Note.txt", "SongEditorTestSongs/Copy-Note-Pasted.txt").Returns(null),
        new TestCaseData("SongEditorTestSongs/Copy-Sentence.txt", "SongEditorTestSongs/Copy-Sentence-Pasted.txt").Returns(null),
        new TestCaseData("SongEditorTestSongs/Copy-Sentences.txt", "SongEditorTestSongs/Copy-Sentences-Pasted.txt").Returns(null),
        new TestCaseData("SongEditorTestSongs/Copy-Voices.txt", "SongEditorTestSongs/Copy-Voices-Pasted.txt").Returns(null),
    };

    [Inject(SearchMethod = SearchMethods.FindObjectOfType)]
    private SongEditorSelectionControl songEditorSelectionControl;

    [Inject(SearchMethod = SearchMethods.FindObjectOfType)]
    private SongEditorCopyPasteManager songEditorCopyPasteManager;

    [Inject(SearchMethod = SearchMethods.FindObjectOfType)]
    private SongEditorSceneInputControl songEditorSceneInputControl;

    [UnityTest]
    [TestCaseSource(nameof(testCases))]
    public IEnumerator CopyAndPasteShouldPreserveNotes(string songFilePath, string expectedSongFilePath) =>
        CopyAndPasteShouldPreserveNotesAsync(songFilePath, expectedSongFilePath);
    private async Awaitable CopyAndPasteShouldPreserveNotesAsync(string songFilePath, string expectedSongFilePath)
    {
        LogAssertUtils.IgnoreFailingMessages();
        await OpenSongEditorWithNewSong(songFilePath);
        await ExpectScene(EScene.SongEditorScene);

        // Select all and copy
        await SelectAll();
        await CopyNotes();

        // Select all again, go to first note, then delete
        await SelectAll();
        await MoveToFirstSelectedNote();
        await DeleteNotes();

        // Paste
        await PasteNotes();

        // Compare to original song because we deleted original notes before re-pasting the same set of notes.
        await ExpectCurrentSongEqualsExpectedResult(songFilePath);
    }

    [UnityTest]
    [TestCaseSource(nameof(testCases))]
    public IEnumerator CopyAndPasteShouldAddNotesAndPreserveSentences(string songFilePath, string expectedSongFilePath) =>
        CopyAndPasteShouldAddNotesAndPreserveSentencesAsync(songFilePath, expectedSongFilePath);
    private async Awaitable CopyAndPasteShouldAddNotesAndPreserveSentencesAsync(string songFilePath, string expectedSongFilePath)
    {
        LogAssertUtils.IgnoreFailingMessages();
        await OpenSongEditorWithNewSong(songFilePath);
        await ExpectScene(EScene.SongEditorScene);

        // Select all and copy
        await SelectAll();
        await CopyNotes();

        // Go behind last note
        await SelectAll();
        await MoveBehindLastNote();

        // Paste
        await PasteNotes();

        await ExpectCurrentSongEqualsExpectedResult(expectedSongFilePath);
    }

    private async Awaitable MoveToFirstSelectedNote()
    {
        songAudioPlayer.PositionInMillis = GetFirstSelectedNotePositionInMillis();
        await Awaitable.WaitForSecondsAsync(waitTimeInSeconds);
    }

    private async Awaitable MoveBehindLastNote()
    {
        songAudioPlayer.PositionInMillis = GetAfterLastNoteEndPositionInMillis();
        await Awaitable.WaitForSecondsAsync(waitTimeInSeconds);
    }

    private async Awaitable DeleteNotes()
    {
        // TODO: Input simulation does not work reliably for some reason
        // TriggerInputAction(R.InputActions.songEditor_delete);
        songEditorSceneInputControl.DeleteSelectedNotes();
        await Awaitable.WaitForSecondsAsync(waitTimeInSeconds);
    }

    private async Awaitable CopyNotes()
    {
        // TODO: Input simulation does not work reliably for some reason
        // TriggerInputAction(R.InputActions.songEditor_copy);
        songEditorCopyPasteManager.CopySelection();
        await Awaitable.WaitForSecondsAsync(waitTimeInSeconds);
    }

    private async Awaitable PasteNotes()
    {
        // TODO: Input simulation does not work reliably for some reason
        // TriggerInputAction(R.InputActions.songEditor_paste);
        songEditorCopyPasteManager.Paste();
        await Awaitable.WaitForSecondsAsync(waitTimeInSeconds);
    }

    private async Awaitable SelectAll()
    {
        // TODO: Input simulation does not work reliably for some reason
        // TriggerInputAction(R.InputActions.songEditor_selectAll);
        songEditorSelectionControl.SelectAll();
        await WaitForCondition(() => !songEditorSelectionControl.GetSelectedNotes().IsNullOrEmpty());
    }

    private double GetFirstSelectedNotePositionInMillis()
    {
        int positionInBeats = songEditorSelectionControl.GetSelectedNotes().Select(note => note.StartBeat).Min();
        return SongMetaBpmUtils.BeatsToMillis(SongMeta, positionInBeats);
    }

    private double GetAfterLastNoteEndPositionInMillis()
    {
        int positionInBeats = SongMetaUtils.GetAllNotes(SongMeta).Select(note => note.EndBeat).Max() + 1;
        return SongMetaBpmUtils.BeatsToMillis(SongMeta, positionInBeats);
    }
}
