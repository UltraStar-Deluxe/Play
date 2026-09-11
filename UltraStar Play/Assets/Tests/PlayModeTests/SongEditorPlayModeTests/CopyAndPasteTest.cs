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

    // Skip initialization because this test loads the SongEditor in a custom way
    protected override string TestSceneName => "";

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
        await OpenSongEditorWithNewSongAsync(songFilePath);
        await ExpectSceneAsync(EScene.SongEditorScene);

        // Select all and copy
        await SelectAllAsync();
        await CopyNotesAsync();

        // Select all again, go to first note, then delete
        await SelectAllAsync();
        await MoveToFirstSelectedNoteAsync();
        await DeleteNotesAsync();

        // Paste
        await PasteNotesAsync();

        // Compare to original song because we deleted original notes before re-pasting the same set of notes.
        await ExpectCurrentSongEqualsExpectedResultAsync(songFilePath);
    }

    [UnityTest]
    [TestCaseSource(nameof(testCases))]
    public IEnumerator CopyAndPasteShouldAddNotesAndPreserveSentences(string songFilePath, string expectedSongFilePath) =>
        CopyAndPasteShouldAddNotesAndPreserveSentencesAsync(songFilePath, expectedSongFilePath);
    private async Awaitable CopyAndPasteShouldAddNotesAndPreserveSentencesAsync(string songFilePath, string expectedSongFilePath)
    {
        LogAssertUtils.IgnoreFailingMessages();
        await OpenSongEditorWithNewSongAsync(songFilePath);
        await ExpectSceneAsync(EScene.SongEditorScene);

        // Select all and copy
        await SelectAllAsync();
        await CopyNotesAsync();

        // Go behind last note
        await SelectAllAsync();
        await MoveBehindLastNoteAsync();

        // Paste
        await PasteNotesAsync();

        await ExpectCurrentSongEqualsExpectedResultAsync(expectedSongFilePath);
    }

    private async Awaitable MoveToFirstSelectedNoteAsync()
    {
        songAudioPlayer.PositionInMillis = GetFirstSelectedNotePositionInMillis();
        await Awaitable.WaitForSecondsAsync(waitTimeInSeconds);
    }

    private async Awaitable MoveBehindLastNoteAsync()
    {
        songAudioPlayer.PositionInMillis = GetAfterLastNoteEndPositionInMillis();
        await Awaitable.WaitForSecondsAsync(waitTimeInSeconds);
    }

    private async Awaitable DeleteNotesAsync()
    {
        // TODO: Input simulation does not work reliably for some reason
        // TriggerInputAction(R.InputActions.songEditor_delete);
        songEditorSceneInputControl.DeleteSelectedNotes();
        await Awaitable.WaitForSecondsAsync(waitTimeInSeconds);
    }

    private async Awaitable CopyNotesAsync()
    {
        // TODO: Input simulation does not work reliably for some reason
        // TriggerInputAction(R.InputActions.songEditor_copy);
        songEditorCopyPasteManager.CopySelection();
        await Awaitable.WaitForSecondsAsync(waitTimeInSeconds);
    }

    private async Awaitable PasteNotesAsync()
    {
        // TODO: Input simulation does not work reliably for some reason
        // TriggerInputAction(R.InputActions.songEditor_paste);
        songEditorCopyPasteManager.Paste();
        await Awaitable.WaitForSecondsAsync(waitTimeInSeconds);
    }

    private async Awaitable SelectAllAsync()
    {
        // TODO: Input simulation does not work reliably for some reason
        // TriggerInputAction(R.InputActions.songEditor_selectAll);
        songEditorSelectionControl.SelectAll();
        await WaitForConditionAsync(() => !songEditorSelectionControl.GetSelectedNotes().IsNullOrEmpty());
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
