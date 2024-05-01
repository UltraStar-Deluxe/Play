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

        contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_fitSentenceToNotes), () => sentenceFitToNoteAction.ExecuteAndNotify(selectedSentences));
        contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_fitSentenceToNotesForAllSentences), () => sentenceFitToNoteAction.ExecuteAndNotify(SongMetaUtils.GetAllSentences(songMeta)));
        contextMenu.AddSeparator();
        contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_editLyrics), () => sentenceControl.StartEditingLyrics());
        contextMenu.AddSeparator();
        contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_speechRecognitionOnAudio,
                "audio", settings.SongEditorSettings.SpeechRecognitionSamplesSource),
            () => speechRecognitionAction.SetTextToAnalyzedSpeech(sentenceControl.Sentence.Notes.ToList(), settings.SongEditorSettings.SpeechRecognitionSamplesSource, true));
        contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_moveToDetectedPitch),
            () => pitchDetectionAction.MoveNotesToDetectedPitchUsingPitchDetectionLayer(sentenceControl.Sentence.Notes.ToList(),true));
        contextMenu.AddSeparator();
        contextMenu.AddButton(Translation.Get(R.Messages.action_delete), () => deleteSentencesAction.ExecuteAndNotify(selectedSentences));
    }
}
