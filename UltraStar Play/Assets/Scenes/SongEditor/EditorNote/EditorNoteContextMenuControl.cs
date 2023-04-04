using System.Collections.Generic;
using System.Linq;
using UniInject;
using Vosk;

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
    private MoveNoteToAjacentSentenceAction moveNoteToAdjacentSentenceAction;

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

        contextMenu.AddButton("Edit lyrics", () => songEditorSceneControl.StartEditingSelectedNoteText());
        FillContextMenuToSplitAndMergeNotes(contextMenu, selectedNotes);
        FillContextMenuForAiTools(contextMenu, selectedNotes);
        FillContextMenuToAddSpaceBetweenNotes(contextMenu);
        FillContextMenuToSetNoteType(contextMenu, selectedNotes);
        FillContextMenuToMergeSentences(contextMenu, selectedNotes);
        FillContextMenuToMoveToOtherSentence(contextMenu, selectedNotes);
        FillContextMenuToMoveToOtherVoice(contextMenu, selectedNotes);
        FillContextMenuToDeleteNotes(contextMenu, selectedNotes);
    }

    private void FillContextMenuForAiTools(ContextMenuPopupControl contextMenu, List<Note> selectedNotes)
    {
        int minBeat = selectedNotes.Select(note => note.StartBeat).Min();
        int maxBeat = selectedNotes.Select(note => note.EndBeat).Max();
        int lengthInBeats = maxBeat - minBeat;

        contextMenu.AddSeparator();
        contextMenu.AddButton("Speech recognition to set lyrics", () => speechRecognitionAction.SetTextToAnalyzedSpeech(selectedNotes, true));
        contextMenu.AddButton("Speech recognition to create notes", () =>
        {
            SpeechRecognitionParameters speechRecognitionParameters = speechRecognitionAction.CreateSpeechRecognizerParameters();
            VoskRecognizer speechRecognizer = speechRecognitionManager.CreateSpeechRecognizer(speechRecognitionParameters);
            speechRecognitionAction.CreateNotesFromSpeechRecognition(minBeat, lengthInBeats, settings.SongEditorSettings.SpeechRecognitionSamplesSource, 2, true, speechRecognitionParameters, speechRecognizer, false);
        });
        contextMenu.AddButton("Pitch detection", () => pitchDetectionAction.CreateNotesForDetectedPitch(minBeat, lengthInBeats, true));
        contextMenu.AddButton("Move to detected pitch", () => pitchDetectionAction.MoveNotesToDetectedPitch(selectedNotes, true, settings.SongEditorSettings.PitchDetectionSamplesSource));
    }

    private void FillContextMenuToAddSpaceBetweenNotes(ContextMenuPopupControl contextMenu)
    {
        contextMenu.AddSeparator();
        contextMenu.AddButton("Add space between notes", () => CreateAddSpaceBetweenNotesDialog());
    }

    private void FillContextMenuToDeleteNotes(ContextMenuPopupControl contextMenu, List<Note> selectedNotes)
    {
        contextMenu.AddSeparator();
        contextMenu.AddButton("Delete", () => deleteNotesAction.ExecuteAndNotify(selectedNotes));
    }

    private void FillContextMenuToSplitAndMergeNotes(ContextMenuPopupControl contextMenu, List<Note> selectedNotes)
    {
        if (splitNotesAction.CanExecute(selectedNotes))
        {
            contextMenu.AddButton("Split Notes", () => splitNotesAction.ExecuteAndNotify(selectedNotes));
        }
        if (mergeNotesAction.CanExecute(selectedNotes))
        {
            contextMenu.AddButton("Merge Notes", () => mergeNotesAction.ExecuteAndNotify(selectedNotes, noteControl.Note));
        }
    }

    private void FillContextMenuToSetNoteType(ContextMenuPopupControl contextMenu, List<Note> selectedNotes)
    {
        contextMenu.AddSeparator();
        if (setNoteTypeAction.CanExecute(selectedNotes, ENoteType.Golden))
        {
            contextMenu.AddButton("Make golden",
                () => setNoteTypeAction.ExecuteAndNotify(selectedNotes, ENoteType.Golden));
        }
        if (setNoteTypeAction.CanExecute(selectedNotes, ENoteType.Freestyle))
        {
            contextMenu.AddButton("Make freestyle",
                () => setNoteTypeAction.ExecuteAndNotify(selectedNotes, ENoteType.Freestyle));
        }
        if (setNoteTypeAction.CanExecute(selectedNotes, ENoteType.Rap))
        {
            contextMenu.AddButton("Make rap",
                () => setNoteTypeAction.ExecuteAndNotify(selectedNotes, ENoteType.Rap));
        }
        if (setNoteTypeAction.CanExecute(selectedNotes, ENoteType.RapGolden))
        {
            contextMenu.AddButton("Make rap-golden",
                () => setNoteTypeAction.ExecuteAndNotify(selectedNotes, ENoteType.RapGolden));
        }
        if (setNoteTypeAction.CanExecute(selectedNotes, ENoteType.Normal))
        {
            contextMenu.AddButton("Make normal",
                () => setNoteTypeAction.ExecuteAndNotify(selectedNotes, ENoteType.Normal));
        }
    }

    private void FillContextMenuToMergeSentences(ContextMenuPopupControl contextMenu, List<Note> selectedNotes)
    {
        if (mergeSentencesAction.CanExecute(selectedNotes))
        {
            contextMenu.AddSeparator();
            contextMenu.AddButton("Merge sentences",
                () => mergeSentencesAction.ExecuteAndNotify(selectedNotes, noteControl.Note));
        }
    }

    private void FillContextMenuToMoveToOtherVoice(ContextMenuPopupControl contextMenu, List<Note> selectedNotes)
    {
        bool canMoveToVoice1 = moveNotesToOtherVoiceAction.CanMoveNotesToVoice(selectedNotes, Voice.soloVoiceName, Voice.firstVoiceName);
        bool canMoveToVoice2 = moveNotesToOtherVoiceAction.CanMoveNotesToVoice(selectedNotes, Voice.secondVoiceName);
        if (canMoveToVoice1)
        {
            contextMenu.AddSeparator();
            contextMenu.AddButton("Assign to player 1",
                () => moveNotesToOtherVoiceAction.MoveNotesToVoiceAndNotify(songMeta, selectedNotes, Voice.firstVoiceName));
        }
        if (!canMoveToVoice1 && canMoveToVoice2)
        {
            contextMenu.AddSeparator();
        }
        if (canMoveToVoice2)
        {
            contextMenu.AddButton("Assign to player 2",
                () => moveNotesToOtherVoiceAction.MoveNotesToVoiceAndNotify(songMeta, selectedNotes, Voice.secondVoiceName));
        }

        if (moveNoteToOwnSentenceAction.CanMoveToOwnSentence(selectedNotes))
        {
            contextMenu.AddButton("Assign to own sentence", () => moveNoteToOwnSentenceAction.MoveToOwnSentenceAndNotify(selectedNotes));
        }
    }

    private void FillContextMenuToMoveToOtherSentence(ContextMenuPopupControl contextMenu, List<Note> selectedNotes)
    {
        bool canMoveToPreviousSentence = moveNoteToAdjacentSentenceAction.CanMoveToPreviousSentence(selectedNotes, noteControl.Note);
        bool canMoveToNextSentence = moveNoteToAdjacentSentenceAction.CanMoveToNextSentence(selectedNotes, noteControl.Note);
        if (canMoveToPreviousSentence)
        {
            contextMenu.AddSeparator();
            contextMenu.AddButton("Move to previous sentence",
                () => moveNoteToAdjacentSentenceAction.MoveToPreviousSentenceAndNotify(noteControl.Note));
        }
        if (!canMoveToPreviousSentence && canMoveToNextSentence)
        {
            contextMenu.AddSeparator();
        }
        if (canMoveToNextSentence)
        {
            contextMenu.AddButton("Move to next sentence",
                () => moveNoteToAdjacentSentenceAction.MoveToNextSentenceAndNotify(noteControl.Note));
        }
    }

    private void CreateAddSpaceBetweenNotesDialog()
    {
        void DoAddSpaceBetweenNotes(int spaceInBeats)
        {
            List<Note> selectedNotes = selectionControl.GetSelectedNotes();
            if (selectedNotes.IsNullOrEmpty())
            {
                // Perform on all notes, but per voice
                songMeta.GetVoices()
                    .ForEach(voice => spaceBetweenNotesAction.ExecuteAndNotify(SongMetaUtils.GetAllNotes(voice), spaceInBeats));
            }
            else
            {
                spaceBetweenNotesAction.ExecuteAndNotify(selectedNotes, spaceInBeats);
            }
        }

        songEditorSceneControl.CreateNumberInputDialog("Add space between notes",
            "Enter the number of beats that should be the minimal distance between adjacent notes.",
            spaceInBeats => DoAddSpaceBetweenNotes((int)spaceInBeats));
    }
}
