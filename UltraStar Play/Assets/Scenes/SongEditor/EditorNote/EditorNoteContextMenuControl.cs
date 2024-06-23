using System.Collections.Generic;
using System.Linq;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class EditorNoteContextMenuControl : ContextMenuControl
{
    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongEditorSelectionControl selectionControl;

    [Inject]
    private DeleteNotesAction deleteNotesAction;

    [Inject]
    private SplitNotesAction splitNotesAction;

    [Inject]
    private MergeNotesAction mergeNotesAction;

    [Inject]
    private MergeSentencesAction mergeSentencesAction;

    [Inject]
    private SetNoteTypeAction setNoteTypeAction;

    [Inject]
    private MoveNoteToAdjacentSentenceAction moveNoteToAdjacentSentenceAction;

    [Inject]
    private MoveNotesToOtherVoiceAction moveNotesToOtherVoiceAction;

    [Inject]
    private MoveNoteToOwnSentenceAction moveNoteToOwnSentenceAction;

    [Inject]
    private SpaceBetweenNotesAction spaceBetweenNotesAction;

    [Inject]
    private PitchDetectionAction pitchDetectionAction;

    [Inject]
    private SpeechRecognitionAction speechRecognitionAction;

    [Inject]
    private SpeechRecognitionManager speechRecognitionManager;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private EditorNoteControl noteControl;

    [Inject]
    private Settings settings;

    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();
        FillContextMenuAction = FillContextMenu;
        ShouldOpenContextMenuFunction = ShouldOpenContextMenu;
    }

    private bool ShouldOpenContextMenu()
    {
        return noteControl.Note.IsEditable;
    }

    private void FillContextMenu(ContextMenuPopupControl contextMenu)
    {
        if (!noteControl.Note.IsEditable)
        {
            return;
        }

        if (!selectionControl.IsSelected(noteControl.Note))
        {
            selectionControl.SetSelection(new List<EditorNoteControl> { noteControl });
        }

        List<Note> selectedNotes = selectionControl.GetSelectedNotes();
        if (selectedNotes.IsNullOrEmpty())
        {
            return;
        }

        contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_editLyrics), () => songEditorSceneControl.StartEditingSelectedNoteText());
        FillContextMenuForAiTools(contextMenu, selectedNotes);
        FillContextMenuToMergeAndAddSpaceBetweenNotes(contextMenu, selectedNotes);
        FillContextMenuToSetNoteType(contextMenu, selectedNotes);
        FillContextMenuToMergeSentences(contextMenu, selectedNotes);
        FillContextMenuToMoveToOtherSentenceOrVoice(contextMenu, selectedNotes);
        FillContextMenuToDeleteNotes(contextMenu, selectedNotes);
    }

    private void FillContextMenuForAiTools(ContextMenuPopupControl contextMenu, List<Note> selectedNotes)
    {
        int minBeat = selectedNotes.Select(note => note.StartBeat).Min();
        int maxBeat = selectedNotes.Select(note => note.EndBeat).Max();
        int lengthInBeats = maxBeat - minBeat;

        contextMenu.AddSeparator();

        contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_speechRecognitionOnAudio,
                "audio", settings.SongEditorSettings.SpeechRecognitionSamplesSource),
            () => speechRecognitionAction.SetTextToAnalyzedSpeech(selectedNotes, settings.SongEditorSettings.SpeechRecognitionSamplesSource, true));
        contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_moveToDetectedPitch),
            () => pitchDetectionAction.MoveNotesToDetectedPitchUsingPitchDetectionLayer(selectedNotes, true));
    }

    private void FillContextMenuToMergeAndAddSpaceBetweenNotes(ContextMenuPopupControl contextMenu, List<Note> selectedNotes)
    {
        contextMenu.AddSeparator();

        if (mergeNotesAction.CanExecute(selectedNotes))
        {
            contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_mergeNotes), () => mergeNotesAction.ExecuteAndNotify(selectedNotes, noteControl.Note));
        }
    }

    private void FillContextMenuToDeleteNotes(ContextMenuPopupControl contextMenu, List<Note> selectedNotes)
    {
        contextMenu.AddSeparator();
        contextMenu.AddButton(Translation.Get(R.Messages.action_delete), () => deleteNotesAction.ExecuteAndNotify(selectedNotes));
    }

    private void FillContextMenuToSetNoteType(ContextMenuPopupControl contextMenu, List<Note> selectedNotes)
    {
        contextMenu.AddSeparator();
        if (setNoteTypeAction.CanExecute(selectedNotes, ENoteType.Golden))
        {
            contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_setNoteTypeGolden),
                () => setNoteTypeAction.ExecuteAndNotify(selectedNotes, ENoteType.Golden));
        }
        if (setNoteTypeAction.CanExecute(selectedNotes, ENoteType.Freestyle))
        {
            contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_setNoteTypeFreestyle),
                () => setNoteTypeAction.ExecuteAndNotify(selectedNotes, ENoteType.Freestyle));
        }
        if (setNoteTypeAction.CanExecute(selectedNotes, ENoteType.Rap))
        {
            contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_setNoteTypeRap),
                () => setNoteTypeAction.ExecuteAndNotify(selectedNotes, ENoteType.Rap));
        }
        if (setNoteTypeAction.CanExecute(selectedNotes, ENoteType.RapGolden))
        {
            contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_setNoteTypeRapGolden),
                () => setNoteTypeAction.ExecuteAndNotify(selectedNotes, ENoteType.RapGolden));
        }
        if (setNoteTypeAction.CanExecute(selectedNotes, ENoteType.Normal))
        {
            contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_setNoteTypeNormal),
                () => setNoteTypeAction.ExecuteAndNotify(selectedNotes, ENoteType.Normal));
        }
    }

    private void FillContextMenuToMergeSentences(ContextMenuPopupControl contextMenu, List<Note> selectedNotes)
    {
        if (mergeSentencesAction.CanExecute(selectedNotes))
        {
            contextMenu.AddSeparator();
            contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_mergeSentences),
                () => mergeSentencesAction.ExecuteAndNotify(selectedNotes, noteControl.Note));
        }
    }

    private void FillContextMenuToMoveToOtherSentenceOrVoice(ContextMenuPopupControl contextMenu, List<Note> selectedNotes)
    {
        bool canMoveToVoice1 = moveNotesToOtherVoiceAction.CanMoveNotesToVoice(selectedNotes, EVoiceId.P1);
        bool canMoveToVoice2 = moveNotesToOtherVoiceAction.CanMoveNotesToVoice(selectedNotes, EVoiceId.P2);
        if (canMoveToVoice1)
        {
            contextMenu.AddSeparator();
            contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_assignToVoice, "name", "1"),
                () => moveNotesToOtherVoiceAction.MoveNotesToVoiceAndNotify(songMeta, selectedNotes, EVoiceId.P1));
        }
        if (!canMoveToVoice1 && canMoveToVoice2)
        {
            contextMenu.AddSeparator();
        }
        if (canMoveToVoice2)
        {
            contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_assignToVoice, "name", "2"),
                () => moveNotesToOtherVoiceAction.MoveNotesToVoiceAndNotify(songMeta, selectedNotes, EVoiceId.P2));
        }

        if (moveNoteToOwnSentenceAction.CanMoveToOwnSentence(selectedNotes))
        {
            contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_moveNotesToOwnSentence), () => moveNoteToOwnSentenceAction.MoveToOwnSentenceAndNotify(selectedNotes));
        }

        bool canMoveToPreviousSentence = moveNoteToAdjacentSentenceAction.CanMoveToPreviousSentence(selectedNotes, noteControl.Note);
        bool canMoveToNextSentence = moveNoteToAdjacentSentenceAction.CanMoveToNextSentence(selectedNotes, noteControl.Note);
        if (canMoveToPreviousSentence)
        {
            contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_moveNotesToPreviousSentence),
                () => moveNoteToAdjacentSentenceAction.MoveToPreviousSentenceAndNotify(selectedNotes));
        }
        if (canMoveToNextSentence)
        {
            contextMenu.AddButton(Translation.Get(R.Messages.songEditor_action_moveNotesToNextSentence),
                () => moveNoteToAdjacentSentenceAction.MoveToNextSentenceAndNotify(selectedNotes));
        }
    }
}
