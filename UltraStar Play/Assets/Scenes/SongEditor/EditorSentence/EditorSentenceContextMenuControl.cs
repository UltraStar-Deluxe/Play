using System.Collections.Generic;
using System.Linq;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class EditorSentenceContextMenuControl : ContextMenuControl
{
    [Inject]
    private DeleteSentencesAction deleteSentencesAction;

    [Inject]
    private SentenceFitToNoteAction sentenceFitToNoteAction;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SpeechRecognitionAction speechRecognitionAction;

    [Inject]
    private PitchDetectionAction pitchDetectionAction;

    [Inject]
    private EditorSentenceControl sentenceControl;

    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();
        FillContextMenuAction = FillContextMenu;
    }

    private void FillContextMenu(ContextMenuPopupControl contextMenu)
    {
        List<Sentence> selectedSentences = new() { sentenceControl.Sentence };
        int minBeat = sentenceControl.Sentence.MinBeat;
        int maxBeat = sentenceControl.Sentence.ExtendedMaxBeat;
        int lengthInBeats = maxBeat - minBeat;
        int extendedSentenceLengthInBeats = sentenceControl.Sentence.ExtendedMaxBeat - sentenceControl.Sentence.MinBeat;

        contextMenu.AddItem("Fit to notes", () => sentenceFitToNoteAction.ExecuteAndNotify(selectedSentences));
        contextMenu.AddItem("Fit to notes (all phrases)", () => sentenceFitToNoteAction.ExecuteAndNotify(SongMetaUtils.GetAllSentences(songMeta)));
        contextMenu.AddSeparator();
        contextMenu.AddItem("Edit lyrics", () => sentenceControl.StartEditingLyrics());
        contextMenu.AddSeparator();
        contextMenu.AddItem("Speech recognition to set lyrics", () => speechRecognitionAction.SetTextToAnalyzedSpeech(sentenceControl.Sentence.Notes.ToList(), true));
        contextMenu.AddItem("Speech recognition to create notes", () => speechRecognitionAction.CreateNotesFromSpeechRecognition(minBeat, extendedSentenceLengthInBeats, true));
        contextMenu.AddItem("Pitch detection", () => pitchDetectionAction.CreateNotesForDetectedPitch(minBeat, lengthInBeats, true));
        contextMenu.AddSeparator();
        contextMenu.AddItem("Delete", () => deleteSentencesAction.ExecuteAndNotify(selectedSentences));
    }
}
