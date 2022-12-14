using System.Collections.Generic;
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
    private MoveNoteToAjacentSentenceAction moveNoteToAdjacentSentenceAction;

    [Inject]
    private MoveNotesToOtherVoiceAction moveNotesToOtherVoiceAction;

    [Inject]
    private MoveNoteToOwnSentenceAction moveNoteToOwnSentenceAction;

    [Inject]
    private SpaceBetweenNotesAction spaceBetweenNotesAction;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private EditorNoteControl noteControl;

    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();
        FillContextMenuAction = FillContextMenu;
    }

    private void FillContextMenu(ContextMenuPopupControl contextMenu)
    {
        if (!selectionControl.IsSelected(noteControl.Note))
        {
            selectionControl.SetSelection(new List<EditorNoteControl> { noteControl });
        }

        List<Note> selectedNotes = selectionControl.GetSelectedNotes();

        contextMenu.AddItem("Edit lyrics", () => songEditorSceneControl.StartEditingSelectedNoteText());
        FillContextMenuToSplitAndMergeNotes(contextMenu, selectedNotes);
        FillContextMenuToAddSpaceBetweenNotes(contextMenu);
        FillContextMenuToSetNoteType(contextMenu, selectedNotes);
        FillContextMenuToMergeSentences(contextMenu, selectedNotes);
        FillContextMenuToMoveToOtherSentence(contextMenu, selectedNotes);
        FillContextMenuToMoveToOtherVoice(contextMenu, selectedNotes);
        FillContextMenuToDeleteNotes(contextMenu, selectedNotes);
    }

    private void FillContextMenuToAddSpaceBetweenNotes(ContextMenuPopupControl contextMenu)
    {
        contextMenu.AddSeparator();
        contextMenu.AddItem("Add space between notes", () => CreateAddSpaceBetweenNotesDialog());
    }

    private void FillContextMenuToDeleteNotes(ContextMenuPopupControl contextMenu, List<Note> selectedNotes)
    {
        contextMenu.AddSeparator();
        contextMenu.AddItem("Delete", () => deleteNotesAction.ExecuteAndNotify(selectedNotes));
    }

    private void FillContextMenuToSplitAndMergeNotes(ContextMenuPopupControl contextMenu, List<Note> selectedNotes)
    {
        if (splitNotesAction.CanExecute(selectedNotes))
        {
            contextMenu.AddItem("Split Notes", () => splitNotesAction.ExecuteAndNotify(selectedNotes));
        }
        if (mergeNotesAction.CanExecute(selectedNotes))
        {
            contextMenu.AddItem("Merge Notes", () => mergeNotesAction.ExecuteAndNotify(selectedNotes, noteControl.Note));
        }
    }

    private void FillContextMenuToSetNoteType(ContextMenuPopupControl contextMenu, List<Note> selectedNotes)
    {
        contextMenu.AddSeparator();
        if (setNoteTypeAction.CanExecute(selectedNotes, ENoteType.Golden))
        {
            contextMenu.AddItem("Make golden",
                () => setNoteTypeAction.ExecuteAndNotify(selectedNotes, ENoteType.Golden));
        }
        if (setNoteTypeAction.CanExecute(selectedNotes, ENoteType.Freestyle))
        {
            contextMenu.AddItem("Make freestyle",
                () => setNoteTypeAction.ExecuteAndNotify(selectedNotes, ENoteType.Freestyle));
        }
        if (setNoteTypeAction.CanExecute(selectedNotes, ENoteType.Rap))
        {
            contextMenu.AddItem("Make rap",
                () => setNoteTypeAction.ExecuteAndNotify(selectedNotes, ENoteType.Rap));
        }
        if (setNoteTypeAction.CanExecute(selectedNotes, ENoteType.RapGolden))
        {
            contextMenu.AddItem("Make rap-golden",
                () => setNoteTypeAction.ExecuteAndNotify(selectedNotes, ENoteType.RapGolden));
        }
        if (setNoteTypeAction.CanExecute(selectedNotes, ENoteType.Normal))
        {
            contextMenu.AddItem("Make normal",
                () => setNoteTypeAction.ExecuteAndNotify(selectedNotes, ENoteType.Normal));
        }
    }

    private void FillContextMenuToMergeSentences(ContextMenuPopupControl contextMenu, List<Note> selectedNotes)
    {
        if (mergeSentencesAction.CanExecute(selectedNotes))
        {
            contextMenu.AddSeparator();
            contextMenu.AddItem("Merge sentences",
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
            contextMenu.AddItem("Move to player 1",
                () => moveNotesToOtherVoiceAction.MoveNotesToVoiceAndNotify(songMeta, selectedNotes, Voice.firstVoiceName));
        }
        if (!canMoveToVoice1 && canMoveToVoice2)
        {
            contextMenu.AddSeparator();
        }
        if (canMoveToVoice2)
        {
            contextMenu.AddItem("Move to player 2",
                () => moveNotesToOtherVoiceAction.MoveNotesToVoiceAndNotify(songMeta, selectedNotes, Voice.secondVoiceName));
        }

        if (moveNoteToOwnSentenceAction.CanMoveToOwnSentence(selectedNotes))
        {
            contextMenu.AddItem("Move to own sentence", () => moveNoteToOwnSentenceAction.MoveToOwnSentenceAndNotify(selectedNotes));
        }
    }

    private void FillContextMenuToMoveToOtherSentence(ContextMenuPopupControl contextMenu, List<Note> selectedNotes)
    {
        bool canMoveToPreviousSentence = moveNoteToAdjacentSentenceAction.CanMoveToPreviousSentence(selectedNotes, noteControl.Note);
        bool canMoveToNextSentence = moveNoteToAdjacentSentenceAction.CanMoveToNextSentence(selectedNotes, noteControl.Note);
        if (canMoveToPreviousSentence)
        {
            contextMenu.AddSeparator();
            contextMenu.AddItem("Move to previous sentence",
                () => moveNoteToAdjacentSentenceAction.MoveToPreviousSentenceAndNotify(noteControl.Note));
        }
        if (!canMoveToPreviousSentence && canMoveToNextSentence)
        {
            contextMenu.AddSeparator();
        }
        if (canMoveToNextSentence)
        {
            contextMenu.AddItem("Move to next sentence",
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
