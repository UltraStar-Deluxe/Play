using System;
using System.Collections;
using System.Collections.Generic;
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

    public void Update()
    {
        bool noModifier = !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftAlt);
        bool ctrlExclusive = Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftAlt);
        bool shiftExclusive = !Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftAlt);
        bool altExclusive = !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.LeftAlt);

        bool ctrlShiftExclusive = Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.LeftAlt);

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
            if (noModifier)
            {
                selectionController.SelectNextNote();
            }
            else if (shiftExclusive)
            {
                selectionController.SelectPreviousNote();
            }
        }

        // Move and stretch notes
        Vector2 arrowKeyDirection = GetArrowKeyDirection();
        if (arrowKeyDirection != Vector2.zero)
        {
            List<Note> selectedNotes = selectionController.GetSelectedNotes();
            foreach (Note note in selectedNotes)
            {
                // Move with Shift
                if (shiftExclusive)
                {
                    int newStartBeat = note.StartBeat + (int)arrowKeyDirection.x;
                    int newEndBeat = note.EndBeat + (int)arrowKeyDirection.x;
                    note.SetStartAndEndBeat(newStartBeat, newEndBeat);

                    int newMidiNote = note.MidiNote + (int)arrowKeyDirection.y;
                    note.SetMidiNote(newMidiNote);
                }

                // Extend right side with Alt
                if (altExclusive)
                {
                    int newEndBeat = note.EndBeat + (int)arrowKeyDirection.x;
                    if (newEndBeat > note.StartBeat)
                    {
                        note.SetEndBeat(newEndBeat);
                    }
                }

                // Extend left side with Ctrl
                if (ctrlExclusive)
                {
                    int newStartBeat = note.StartBeat + (int)arrowKeyDirection.x;
                    if (newStartBeat < note.EndBeat)
                    {
                        note.SetStartBeat(newStartBeat);
                    }
                }
            }
            editorNoteDisplayer.UpdateNotesAndSentences();
        }

        // Scroll and zoom in NoteArea
        if (scrollDirection != 0 && noteArea.IsPointerOver)
        {
            // Scroll horizontal in NoteArea with mouse wheel
            if (noModifier)
            {
                noteArea.ScrollHorizontal(scrollDirection);
            }

            // Zoom horizontal in NoteArea with Ctrl + mouse wheel
            if (ctrlExclusive)
            {
                noteArea.ZoomHorizontal(scrollDirection);
            }

            // Scroll vertical in NoteArea with Shift + mouse wheel
            if (shiftExclusive)
            {
                noteArea.ScrollVertical(scrollDirection);
            }

            // Zoom vertical in NoteArea with Ctrl + Shift + mouse wheel
            if (ctrlShiftExclusive)
            {
                noteArea.ZoomVertical(scrollDirection);
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
