using System.Collections.Generic;
using System.Linq;
using UniInject;
using Vosk;

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
    private SpeechRecognitionManager speechRecognitionManager;

    [Inject]
    private PitchDetectionAction pitchDetectionAction;

    [Inject]
    private EditorSentenceControl sentenceControl;

    [Inject]
    private Settings settings;

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

        contextMenu.AddButton("Fit to notes", () => sentenceFitToNoteAction.ExecuteAndNotify(selectedSentences));
        contextMenu.AddButton("Fit to notes (all phrases)", () => sentenceFitToNoteAction.ExecuteAndNotify(SongMetaUtils.GetAllSentences(songMeta)));
        contextMenu.AddSeparator();
        contextMenu.AddButton("Edit lyrics", () => sentenceControl.StartEditingLyrics());
        contextMenu.AddSeparator();
        contextMenu.AddButton("Speech recognition to set lyrics", () => speechRecognitionAction.SetTextToAnalyzedSpeech(sentenceControl.Sentence.Notes.ToList(), true));
        contextMenu.AddButton("Speech recognition to create notes", () =>
        {
            SpeechRecognitionParameters speechRecognitionParameters = speechRecognitionAction.CreateSpeechRecognizerParameters();
            VoskRecognizer speechRecognizer = speechRecognitionManager.CreateSpeechRecognizer(speechRecognitionParameters);
            speechRecognitionAction.CreateNotesFromSpeechRecognition(minBeat, extendedSentenceLengthInBeats, settings.SongEditorSettings.SpeechRecognitionSamplesSource, 2, true, speechRecognitionParameters, speechRecognizer, false);
        });
        contextMenu.AddButton("Pitch detection", () => pitchDetectionAction.CreateNotesForDetectedPitch(minBeat, lengthInBeats, true));
        contextMenu.AddSeparator();
        contextMenu.AddButton("Delete", () => deleteSentencesAction.ExecuteAndNotify(selectedSentences));
    }
}
