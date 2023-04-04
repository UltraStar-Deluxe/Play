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

        contextMenu.AddButton("Fit to notes", () => sentenceFitToNoteAction.ExecuteAndNotify(selectedSentences));
        contextMenu.AddButton("Fit to notes (all phrases)", () => sentenceFitToNoteAction.ExecuteAndNotify(SongMetaUtils.GetAllSentences(songMeta)));
        contextMenu.AddSeparator();
        contextMenu.AddButton("Edit lyrics", () => sentenceControl.StartEditingLyrics());
        contextMenu.AddSeparator();
        contextMenu.AddButton("Speech recognition to set lyrics", () => speechRecognitionAction.SetTextToAnalyzedSpeech(sentenceControl.Sentence.Notes.ToList(), settings.SongEditorSettings.SpeechRecognitionSamplesSource, true));
        contextMenu.AddButton("Pitch detection", () => pitchDetectionAction.MoveNotesToDetectedPitch(sentenceControl.Sentence.Notes.ToList(),true, settings.SongEditorSettings.PitchDetectionSamplesSource));
        contextMenu.AddSeparator();
        contextMenu.AddButton("Delete", () => deleteSentencesAction.ExecuteAndNotify(selectedSentences));
    }
}
