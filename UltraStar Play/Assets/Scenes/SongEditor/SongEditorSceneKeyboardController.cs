using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;

#pragma warning disable CS0649

public class SongEditorSceneKeyboardController : MonoBehaviour, INeedInjection
{

    [Inject(searchMethod = SearchMethods.FindObjectOfType)]
    private SongEditorSceneController songEditorSceneController;

    [Inject(searchMethod = SearchMethods.FindObjectOfType)]
    private NoteArea noteArea;

    [Inject]
    private SongEditorSelectionController selectionController;

    [Inject]
    private EditorNoteDisplayer editorNoteDisplayer;

    [Inject]
    private Settings settings;

    public void Update()
    {
        EKeyboardModifier modifier = InputUtils.GetCurrentKeyboardModifier();

        int scrollDirection = Math.Sign(Input.mouseScrollDelta.y);

        if (Input.GetKeyUp(KeyCode.Space))
        {
            songEditorSceneController.TogglePlayPause();
        }

        if (Input.GetKeyUp(KeyCode.Delete))
        {
            List<Note> selectedNotes = selectionController.GetSelectedNotes();
            songEditorSceneController.DeleteNotes(selectedNotes);
        }

        // Tab to select next note, Shift+Tab to select previous note
        if (Input.GetKeyUp(KeyCode.Tab))
        {
            if (modifier == EKeyboardModifier.None)
            {
                selectionController.SelectNextNote();
            }
            else if (modifier == EKeyboardModifier.Shift)
            {
                selectionController.SelectPreviousNote();
            }
        }

        // Move and stretch notes
        UpdateInputToMoveAndStretchNotes(modifier);

        // Scroll and zoom in NoteArea
        if (scrollDirection != 0 && noteArea.IsPointerOver)
        {
            // Scroll horizontal in NoteArea with mouse wheel
            if (modifier == EKeyboardModifier.None)
            {
                noteArea.ScrollHorizontal(scrollDirection);
            }

            // Zoom horizontal in NoteArea with Ctrl + mouse wheel
            if (modifier == EKeyboardModifier.Ctrl)
            {
                noteArea.ZoomHorizontal(scrollDirection);
            }

            // Scroll vertical in NoteArea with Shift + mouse wheel
            if (modifier == EKeyboardModifier.Shift)
            {
                noteArea.ScrollVertical(scrollDirection);
            }

            // Zoom vertical in NoteArea with Ctrl + Shift + mouse wheel
            if (modifier == EKeyboardModifier.CtrlShift)
            {
                noteArea.ZoomVertical(scrollDirection);
            }
        }
    }

    private void UpdateInputToMoveAndStretchNotes(EKeyboardModifier modifier)
    {
        Vector2 arrowKeyDirection = GetArrowKeyDirection();
        if (arrowKeyDirection == Vector2.zero)
        {
            return;
        }

        List<Note> selectedNotes = selectionController.GetSelectedNotes();
        if (selectedNotes.IsNullOrEmpty())
        {
            return;
        }

        foreach (Note note in selectedNotes)
        {
            // Move with Shift
            if (modifier == EKeyboardModifier.Shift)
            {
                note.MoveHorizontal((int)arrowKeyDirection.x);
                note.MoveVertical((int)arrowKeyDirection.y);
            }

            // Extend right side with Alt
            if (modifier == EKeyboardModifier.Alt)
            {
                int newEndBeat = note.EndBeat + (int)arrowKeyDirection.x;
                if (newEndBeat > note.StartBeat)
                {
                    note.SetEndBeat(newEndBeat);
                }
            }

            // Extend left side with Ctrl
            if (modifier == EKeyboardModifier.Ctrl)
            {
                int newStartBeat = note.StartBeat + (int)arrowKeyDirection.x;
                if (newStartBeat < note.EndBeat)
                {
                    note.SetStartBeat(newStartBeat);
                }
            }

            // Adjust following notes.
            if (settings.SongEditorSettings.AdjustFollowingNotes)
            {
                AdjustFollowingNotes(modifier, arrowKeyDirection, selectedNotes);
            }
        }
        editorNoteDisplayer.UpdateNotesAndSentences();
    }

    private void AdjustFollowingNotes(EKeyboardModifier modifier, Vector2 arrowKeyDirection, List<Note> selectedNotes)
    {
        // Moving is applied to following notes as well.
        // When extending / shrinking the right side, then the following notes are move to compensate.
        List<Note> followingNotes = songEditorSceneController.GetFollowingNotes(selectedNotes);
        foreach (Note note in followingNotes)
        {
            // Moved with Shift. The following notes are moved as well.
            if (modifier == EKeyboardModifier.Shift)
            {
                note.MoveHorizontal((int)arrowKeyDirection.x);
                note.MoveVertical((int)arrowKeyDirection.y);
            }

            // Extended right side with Alt. The following notes must be moved to compensate.
            if (modifier == EKeyboardModifier.Alt)
            {
                note.MoveHorizontal((int)arrowKeyDirection.x);
            }
        }
    }

    private Vector2 GetArrowKeyDirection()
    {
        Vector2 result = Vector2.zero;
        if (Input.GetKeyUp(KeyCode.LeftArrow))
        {
            result += new Vector2(-1, 0);
        }
        if (Input.GetKeyUp(KeyCode.RightArrow))
        {
            result += new Vector2(1, 0);
        }
        if (Input.GetKeyUp(KeyCode.UpArrow))
        {
            result += new Vector2(0, 1);
        }
        if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            result += new Vector2(0, -1);
        }
        return result;
    }
}
