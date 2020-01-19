using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    private SongEditorHistoryManager historyManager;

    [Inject]
    private Settings settings;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private DeleteNotesAction deleteNotesAction;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private EventSystem eventSystem;

    [Inject]
    private ToggleNoteTypeAction toggleNoteTypeAction;

    [Inject]
    private MoveNotesAction moveNotesAction;

    [Inject]
    private ExtendNotesAction extendNotesAction;

    // Unity does not provide Input.anyKeyUp, only Input.anyKey, and Input.anyKeyDown.
    private bool isAnyKey;
    private bool isAnyKeyUp;

    public void Update()
    {
        // Detect isAnyKeyUp
        isAnyKeyUp = false;
        if (Input.anyKey)
        {
            isAnyKey = true;
        }
        else if (isAnyKey)
        {
            isAnyKey = false;
            isAnyKeyUp = true;
        }

        if (GameObjectUtils.InputFieldHasFocus(eventSystem))
        {
            return;
        }

        EKeyboardModifier modifier = InputUtils.GetCurrentKeyboardModifier();

        // Play / pause via Space or P
        bool isPlayPauseButtonUp = Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.P);
        if (isPlayPauseButtonUp && modifier == EKeyboardModifier.None)
        {
            ToggleAudioPlayPause();
        }

        // Play only the selected notes via Ctrl+Space or Ctrl+P
        if (isPlayPauseButtonUp && modifier == EKeyboardModifier.Ctrl)
        {
            List<Note> selectedNotes = selectionController.GetSelectedNotes();
            PlayAudioInRangeOfNotes(selectedNotes);
        }

        // Stop via Escape
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            songAudioPlayer.PauseAudio();
        }

        // Select all notes via Ctrl+A
        if (Input.GetKeyUp(KeyCode.A) && modifier == EKeyboardModifier.Ctrl)
        {
            selectionController.SelectAll();
        }

        // Delete notes
        if (Input.GetKeyUp(KeyCode.Delete))
        {
            List<Note> selectedNotes = selectionController.GetSelectedNotes();
            deleteNotesAction.ExecuteAndNotify(selectedNotes);
        }

        // Undo via Ctrl+Z
        if (Input.GetKeyUp(KeyCode.Z) && modifier == EKeyboardModifier.Ctrl)
        {
            historyManager.Undo();
        }

        // Redo via Ctrl+Y
        if (Input.GetKeyUp(KeyCode.Y) && modifier == EKeyboardModifier.Ctrl)
        {
            historyManager.Redo();
        }

        // Save via Ctrl+S
        if (Input.GetKeyUp(KeyCode.S) && modifier == EKeyboardModifier.Ctrl)
        {
            songEditorSceneController.SaveSong();
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

        // Start editing of lyrics with F2
        if (Input.GetKeyUp(KeyCode.F2))
        {
            List<Note> selectedNotes = selectionController.GetSelectedNotes();
            if (selectedNotes.Count == 1)
            {
                Note selectedNote = selectedNotes.FirstOrDefault();
                EditorUiNote uiNote = editorNoteDisplayer.GetUiNoteForNote(selectedNote);
                if (uiNote != null)
                {
                    uiNote.StartEditingNoteText();
                }
            }
        }

        // Change position in song with Ctrl+ArrowKey
        if (!songAudioPlayer.IsPlaying)
        {
            if (Input.GetKey(KeyCode.LeftArrow) && modifier == EKeyboardModifier.Ctrl)
            {
                songAudioPlayer.PositionInSongInMillis -= 1;
            }
            if (Input.GetKey(KeyCode.RightArrow) && modifier == EKeyboardModifier.Ctrl)
            {
                songAudioPlayer.PositionInSongInMillis += 1;
            }
        }

        // Make golden / freestyle / normal
        UpdateInputToChangeNoteType(modifier);

        // Move and stretch notes
        UpdateInputToMoveAndStretchNotes(modifier);

        // Scroll and zoom in NoteArea
        UpdateInputToScrollAndZoom(modifier);

        // Use the shortcuts that are also used in the YASS song editor.
        UpdateInputForYassShortcuts(modifier);
    }

    private void UpdateInputToChangeNoteType(EKeyboardModifier modifier)
    {
        if (modifier != EKeyboardModifier.None)
        {
            return;
        }

        if (Input.GetKeyUp(KeyCode.G))
        {
            toggleNoteTypeAction.ExecuteAndNotify(selectionController.GetSelectedNotes(), ENoteType.Golden);
        }
        if (Input.GetKeyUp(KeyCode.F))
        {
            toggleNoteTypeAction.ExecuteAndNotify(selectionController.GetSelectedNotes(), ENoteType.Freestyle);
        }
        if (Input.GetKeyUp(KeyCode.N))
        {
            toggleNoteTypeAction.ExecuteAndNotify(selectionController.GetSelectedNotes(), ENoteType.Normal);
        }
        if (Input.GetKeyUp(KeyCode.R))
        {
            toggleNoteTypeAction.ExecuteAndNotify(selectionController.GetSelectedNotes(), ENoteType.Rap);
        }
        if (Input.GetKeyUp(KeyCode.T))
        {
            toggleNoteTypeAction.ExecuteAndNotify(selectionController.GetSelectedNotes(), ENoteType.RapGolden);
        }
    }

    // Implements keyboard shortcuts similar to Yass.
    // See: https://github.com/UltraStar-Deluxe/Play/issues/111
    private void UpdateInputForYassShortcuts(EKeyboardModifier modifier)
    {
        if (modifier != EKeyboardModifier.None)
        {
            return;
        }

        if (!isAnyKeyUp)
        {
            return;
        }

        // 4 and 6 on keypad to move to the previous/next note
        List<Note> selectedNotes = selectionController.GetSelectedNotes();
        List<Note> followingNotes = GetFollowingNotesOrEmptyListIfDeactivated(selectedNotes);
        if (Input.GetKeyUp(KeyCode.Keypad4))
        {
            selectionController.SelectPreviousNote();
        }
        if (Input.GetKeyUp(KeyCode.Keypad6))
        {
            selectionController.SelectNextNote();
        }

        // 1 and 3 moves the note left and right (by one beat, length unchanged)
        if (Input.GetKeyUp(KeyCode.Keypad1))
        {
            moveNotesAction.MoveNotesHorizontalAndNotify(-1, selectedNotes, followingNotes);
        }
        if (Input.GetKeyUp(KeyCode.Keypad3))
        {
            moveNotesAction.MoveNotesHorizontalAndNotify(1, selectedNotes, followingNotes);
        }

        // 7 and 9 shortens/lengthens the note (by one beat, on the right side)
        if (Input.GetKeyUp(KeyCode.Keypad7))
        {
            extendNotesAction.ExtendNotesRightAndNotify(-1, selectedNotes, followingNotes);
        }
        if (Input.GetKeyUp(KeyCode.Keypad9))
        {
            extendNotesAction.ExtendNotesRightAndNotify(1, selectedNotes, followingNotes);
        }

        // Minus sign moves a note up a half-tone (due to the key's physical location, this makes sense)
        // Plus sign moves a note down a half-tone
        if (Input.GetKeyUp(KeyCode.KeypadMinus))
        {
            moveNotesAction.MoveNotesVerticalAndNotify(1, selectedNotes, followingNotes);
        }
        if (Input.GetKeyUp(KeyCode.KeypadPlus))
        {
            moveNotesAction.MoveNotesVerticalAndNotify(-1, selectedNotes, followingNotes);
        }

        // 5 plays the current selected note
        if (Input.GetKeyUp(KeyCode.Keypad5))
        {
            PlayAudioInRangeOfNotes(selectedNotes);
        }

        // scroll left with h, scroll right with j
        if (Input.GetKeyUp(KeyCode.H))
        {
            noteArea.ScrollHorizontal(-1);
        }
        if (Input.GetKeyUp(KeyCode.J))
        {
            noteArea.ScrollHorizontal(1);
        }
    }

    private void PlayAudioInRangeOfNotes(List<Note> notes)
    {
        if (songAudioPlayer.IsPlaying)
        {
            return;
        }

        int minBeat = notes.Select(it => it.StartBeat).Min();
        int maxBeat = notes.Select(it => it.EndBeat).Max();
        double maxMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, maxBeat);
        double minMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, minBeat);
        songEditorSceneController.StopPlaybackAfterPositionInSongInMillis = maxMillis;
        songAudioPlayer.PositionInSongInMillis = minMillis;
        songAudioPlayer.PlayAudio();
    }

    private void ToggleAudioPlayPause()
    {
        if (songAudioPlayer.IsPlaying)
        {
            songAudioPlayer.PauseAudio();
        }
        else
        {
            songAudioPlayer.PlayAudio();
        }
    }

    private void UpdateInputToScrollAndZoom(EKeyboardModifier modifier)
    {
        // Scroll with arroy keys
        if (Input.GetKeyUp(KeyCode.LeftArrow) && modifier == EKeyboardModifier.None)
        {
            noteArea.ScrollHorizontal(-1);
        }
        if (Input.GetKeyUp(KeyCode.RightArrow) && modifier == EKeyboardModifier.None)
        {
            noteArea.ScrollHorizontal(1);
        }

        // Zoom horizontal with Ctrl+'+' and Ctrl+'-'
        // Note: On my keyboard, the plus button has KeyCode.Equals but I don't know why.
        bool isPlusKeyUp = Input.GetKeyUp(KeyCode.Plus) || Input.GetKeyUp(KeyCode.KeypadPlus) || Input.GetKeyUp(KeyCode.Equals);
        bool isMinusKeyUp = Input.GetKeyUp(KeyCode.Minus) || Input.GetKeyUp(KeyCode.KeypadMinus);
        if (isPlusKeyUp && modifier == EKeyboardModifier.Ctrl)
        {
            noteArea.ZoomHorizontal(1);
        }
        if (isMinusKeyUp && modifier == EKeyboardModifier.Ctrl)
        {
            noteArea.ZoomHorizontal(-1);
        }

        // Zoom vertical with Ctrl+Shift+'+' and Ctrl+Shift+'-'
        if (isPlusKeyUp && modifier == EKeyboardModifier.CtrlShift)
        {
            noteArea.ZoomVertical(1);
        }
        if (isMinusKeyUp && modifier == EKeyboardModifier.CtrlShift)
        {
            noteArea.ZoomVertical(-1);
        }

        // Zoom and scroll with mouse wheel
        int scrollDirection = Math.Sign(Input.mouseScrollDelta.y);
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
        Vector2 arrowKeyDirection = InputUtils.GetArrowKeyDirection();
        if (arrowKeyDirection == Vector2.zero)
        {
            return;
        }

        List<Note> selectedNotes = selectionController.GetSelectedNotes();
        if (selectedNotes.IsNullOrEmpty())
        {
            return;
        }

        if (modifier == EKeyboardModifier.None)
        {
            return;
        }

        List<Note> followingNotes = GetFollowingNotesOrEmptyListIfDeactivated(selectedNotes);

        // Move with Shift
        if (modifier == EKeyboardModifier.Shift && arrowKeyDirection.x != 0)
        {
            moveNotesAction.MoveNotesHorizontalAndNotify((int)arrowKeyDirection.x, selectedNotes, followingNotes);
        }
        if (modifier == EKeyboardModifier.Shift && arrowKeyDirection.y != 0)
        {
            moveNotesAction.MoveNotesVerticalAndNotify((int)arrowKeyDirection.y, selectedNotes, followingNotes);
        }

        // Move notes one octave up / down via Ctrl+Shift
        if (modifier == EKeyboardModifier.CtrlShift)
        {
            moveNotesAction.MoveNotesVerticalAndNotify((int)arrowKeyDirection.y * 12, selectedNotes, followingNotes);
        }

        // Extend right side with Alt
        if (modifier == EKeyboardModifier.Alt)
        {
            extendNotesAction.ExtendNotesRightAndNotify((int)arrowKeyDirection.x, selectedNotes, followingNotes);
        }

        // Extend left side with Ctrl
        if (modifier == EKeyboardModifier.Ctrl)
        {
            extendNotesAction.ExtendNotesLeftAndNotify((int)arrowKeyDirection.x, selectedNotes);
        }
    }

    private List<Note> GetFollowingNotesOrEmptyListIfDeactivated(List<Note> selectedNotes)
    {

        if (settings.SongEditorSettings.AdjustFollowingNotes)
        {
            return SongMetaUtils.GetFollowingNotes(songMeta, selectedNotes);
        }
        else
        {
            return new List<Note>();
        }
    }
}
