using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UniRx;
using UnityEngine.InputSystem;

#pragma warning disable CS0649

public class SongEditorSceneInputControl : MonoBehaviour, INeedInjection
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

    private bool inputFieldHasFocusOld;

    private void Start()
    {
        // Jump to start / end of song
        InputManager.GetInputAction(R.InputActions.songEditor_jumpToStartOfSong).PerformedAsObservable()
            .Subscribe(_ => songAudioPlayer.PositionInSongInMillis = 0);
        InputManager.GetInputAction(R.InputActions.songEditor_jumpToEndOfSong).PerformedAsObservable()
            .Subscribe(_ => songAudioPlayer.PositionInSongInMillis = songAudioPlayer.DurationOfSongInMillis - 1);
        
        // Play / pause
        InputManager.GetInputAction(R.InputActions.songEditor_togglePlayPause).PerformedAsObservable()
            .Where(_ => !InputUtils.IsKeyboardControlPressed())
            .Subscribe(_ => songEditorSceneController.ToggleAudioPlayPause());

        // Play only the selected notes
        InputManager.GetInputAction(R.InputActions.songEditor_playSelectedNotes).PerformedAsObservable()
            .Subscribe(_ => PlayAudioInRangeOfNotes(selectionController.GetSelectedNotes()));
        
        // Stop playback or return to last scene
        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable()
            .Subscribe(OnBack);
        
        // Select all notes
        InputManager.GetInputAction(R.InputActions.songEditor_selectAll).PerformedAsObservable()
            .Subscribe(_ => selectionController.SelectAll());
        
        // Select next / previous note
        InputManager.GetInputAction(R.InputActions.songEditor_selectNextNote).PerformedAsObservable()
            .Where(_ => !InputUtils.AnyKeyboardModifierPressed())
            .Subscribe(_ => selectionController.SelectNextNote());
        InputManager.GetInputAction(R.InputActions.songEditor_selectPreviousNote).PerformedAsObservable()
            .Subscribe(_ => selectionController.SelectPreviousNote());
        
        // Delete notes
        InputManager.GetInputAction(R.InputActions.songEditor_delete).PerformedAsObservable()
            .Subscribe(_ => deleteNotesAction.ExecuteAndNotify(selectionController.GetSelectedNotes()));
        
        // Undo
        InputManager.GetInputAction(R.InputActions.songEditor_undo).PerformedAsObservable()
            .Subscribe(_ => historyManager.Undo());
        
        // Redo
        InputManager.GetInputAction(R.InputActions.songEditor_redo).PerformedAsObservable()
            .Subscribe(_ => historyManager.Redo());
        
        // Save
        InputManager.GetInputAction(R.InputActions.songEditor_save).PerformedAsObservable()
            .Subscribe(_ => songEditorSceneController.SaveSong());
        
        // Start editing of lyrics
        InputManager.GetInputAction(R.InputActions.songEditor_editLyrics).PerformedAsObservable()
            .Subscribe(_ => StartEditingNoteText());
        
        // Change position in song
        InputManager.GetInputAction(R.InputActions.ui_navigate).PerformedAsObservable()
            .Subscribe(OnNavigate);
        
        // Make golden / freestyle / normal
        InputManager.GetInputAction(R.InputActions.songEditor_toggleNoteTypeGolden).PerformedAsObservable()
            .Where(_ => !InputUtils.AnyKeyboardModifierPressed())
            .Subscribe(_ => toggleNoteTypeAction.ExecuteAndNotify(selectionController.GetSelectedNotes(), ENoteType.Golden));
        
        InputManager.GetInputAction(R.InputActions.songEditor_toggleNoteTypeFreestyle).PerformedAsObservable()
            .Where(_ => !InputUtils.AnyKeyboardModifierPressed())
            .Subscribe(_ => toggleNoteTypeAction.ExecuteAndNotify(selectionController.GetSelectedNotes(), ENoteType.Freestyle));
        
        InputManager.GetInputAction(R.InputActions.songEditor_toggleNoteTypeNormal).PerformedAsObservable()
            .Where(_ => !InputUtils.AnyKeyboardModifierPressed())
            .Subscribe(_ => toggleNoteTypeAction.ExecuteAndNotify(selectionController.GetSelectedNotes(), ENoteType.Normal));
        
        InputManager.GetInputAction(R.InputActions.songEditor_toggleNoteTypeRap).PerformedAsObservable()
            .Where(_ => !InputUtils.AnyKeyboardModifierPressed())
            .Subscribe(_ => toggleNoteTypeAction.ExecuteAndNotify(selectionController.GetSelectedNotes(), ENoteType.Rap));
        
        InputManager.GetInputAction(R.InputActions.songEditor_toggleNoteTypeRapGolden).PerformedAsObservable()
            .Where(_ => !InputUtils.AnyKeyboardModifierPressed())
            .Subscribe(_ => toggleNoteTypeAction.ExecuteAndNotify(selectionController.GetSelectedNotes(), ENoteType.RapGolden));
        
        // Zoom and scroll with mouse wheel
        InputManager.GetInputAction(R.InputActions.ui_scrollWheel).PerformedAsObservable()
            .Subscribe(OnScrollWheel);
        
        // Zoom horizontal with shortcuts
        InputManager.GetInputAction(R.InputActions.songEditor_zoomInHorizontal).PerformedAsObservable()
            .Where(_ => !InputUtils.IsKeyboardShiftPressed())
            .Subscribe(context =>
            {
                noteArea.ZoomHorizontal(1);
            });
        InputManager.GetInputAction(R.InputActions.songEditor_zoomOutHorizontal).PerformedAsObservable()
            .Where(_ => !InputUtils.IsKeyboardShiftPressed())
            .Subscribe(context =>
            {
                noteArea.ZoomHorizontal(-1);
            });

        // Zoom vertical with shortcuts
        InputManager.GetInputAction(R.InputActions.songEditor_zoomInVertical).PerformedAsObservable()
            .Where(_ => InputManager.GetInputAction(R.InputActions.songEditor_zoomOutVertical).ReadValue<float>() == 0)
            .Subscribe(context =>
            {
                noteArea.ZoomVertical(1);
            });
        InputManager.GetInputAction(R.InputActions.songEditor_zoomOutVertical).PerformedAsObservable()
            .Subscribe(context => noteArea.ZoomVertical(-1));
    }

    private void OnBack(InputAction.CallbackContext context)
    {
        if (songAudioPlayer.IsPlaying)
        {
            songAudioPlayer.PauseAudio();
        }
        else if (!inputFieldHasFocusOld)
        {
            // Special case: the back-Action in an InputField removes its focus.
            // Thus also check that in the previous frame no InputField was focused.
            songEditorSceneController.ReturnToLastScene();
        }
    }

    private void OnScrollWheel(InputAction.CallbackContext context)
    {
        EKeyboardModifier modifier = InputUtils.GetCurrentKeyboardModifier();
        
        int scrollDirection = Math.Sign(context.ReadValue<Vector2>().y);
        if (scrollDirection != 0 && noteArea.IsPointerOver)
        {
            // Scroll horizontal in NoteArea with no modifier
            if (modifier == EKeyboardModifier.None)
            {
                noteArea.ScrollHorizontal(scrollDirection);
            }

            // Zoom horizontal in NoteArea with Ctrl
            if (modifier == EKeyboardModifier.Ctrl)
            {
                noteArea.ZoomHorizontal(scrollDirection);
            }

            // Scroll vertical in NoteArea with Shift
            if (modifier == EKeyboardModifier.Shift)
            {
                noteArea.ScrollVertical(scrollDirection);
            }

            // Zoom vertical in NoteArea with Ctrl + Shift
            if (modifier == EKeyboardModifier.CtrlShift)
            {
                noteArea.ZoomVertical(scrollDirection);
            }
        }
    }

    private void OnNavigate(InputAction.CallbackContext context)
    {
        if (!songAudioPlayer.IsPlaying)
        {
            Vector2 direction = context.ReadValue<Vector2>();
            EKeyboardModifier modifier = InputUtils.GetCurrentKeyboardModifier();
            
            // Scroll
            if (modifier == EKeyboardModifier.None)
            {
                if (direction.x < 0)
                {
                    noteArea.ScrollHorizontal(-1);
                }
                if (direction.x > 0)
                {
                    noteArea.ScrollHorizontal(1);
                }
            }
            
            List<Note> selectedNotes = selectionController.GetSelectedNotes();
            if (selectedNotes.IsNullOrEmpty())
            {
                return;
            }
            
            // Move and stretch notes
            List<Note> followingNotes = GetFollowingNotesOrEmptyListIfDeactivated(selectedNotes);

            // Move with Shift
            if (modifier == EKeyboardModifier.Shift)
            {
                if (direction.x != 0)
                {
                    moveNotesAction.MoveNotesHorizontalAndNotify((int)direction.x, selectedNotes, followingNotes);
                }
                if (direction.y != 0)
                {
                    moveNotesAction.MoveNotesVerticalAndNotify((int)direction.y, selectedNotes, followingNotes);
                }
            }

            // Move notes one octave up / down via Ctrl+Shift
            if (modifier == EKeyboardModifier.CtrlShift)
            {
                moveNotesAction.MoveNotesVerticalAndNotify((int)direction.y * 12, selectedNotes, followingNotes);
            }

            // Extend right side with Alt
            if (modifier == EKeyboardModifier.Alt)
            {
                extendNotesAction.ExtendNotesRightAndNotify((int)direction.x, selectedNotes, followingNotes);
            }

            // Extend left side with Ctrl
            if (modifier == EKeyboardModifier.Ctrl)
            {
                extendNotesAction.ExtendNotesLeftAndNotify((int)direction.x, selectedNotes);
            }
        }
    }

    private void Update()
    {
        bool inputFieldHasFocus = GameObjectUtils.InputFieldHasFocus(eventSystem);
        if (inputFieldHasFocus)
        {
            inputFieldHasFocusOld = true;
            return;
        }

        // Move playback position in small steps by holding Ctrl when no note is selected
        if (Keyboard.current != null
            && InputUtils.IsKeyboardControlPressed()
            && (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            && selectionController.GetSelectedNotes().IsNullOrEmpty())
        {
            if (Keyboard.current.leftArrowKey.isPressed)
            {
                songAudioPlayer.PositionInSongInMillis -= 1;
            }
            else if (Keyboard.current.rightArrowKey.isPressed)
            {
                songAudioPlayer.PositionInSongInMillis += 1;
            }
        }
        
        // Use the shortcuts that are also used in the YASS song editor.
        UpdateInputForYassShortcuts();

        inputFieldHasFocusOld = inputFieldHasFocus;
    }

    // Implements keyboard shortcuts similar to Yass.
    // See: https://github.com/UltraStar-Deluxe/Play/issues/111
    private void UpdateInputForYassShortcuts()
    {
        EKeyboardModifier modifier = InputUtils.GetCurrentKeyboardModifier();
        if (modifier != EKeyboardModifier.None
            // Yass shortcuts only work with a keyboard.
            || Keyboard.current == null)
        {
            return;
        }
        
        // 4 and 6 on keypad to move to the previous/next note
        List<Note> selectedNotes = selectionController.GetSelectedNotes();
        List<Note> followingNotes = GetFollowingNotesOrEmptyListIfDeactivated(selectedNotes);
        if (Keyboard.current.numpad4Key.wasReleasedThisFrame)
        {
            selectionController.SelectPreviousNote();
        }
        if (Keyboard.current.numpad6Key.wasReleasedThisFrame)
        {
            selectionController.SelectNextNote();
        }

        // 1 and 3 moves the note left and right (by one beat, length unchanged)
        if (Keyboard.current.numpad1Key.wasReleasedThisFrame)
        {
            moveNotesAction.MoveNotesHorizontalAndNotify(-1, selectedNotes, followingNotes);
        }
        if (Keyboard.current.numpad3Key.wasReleasedThisFrame)
        {
            moveNotesAction.MoveNotesHorizontalAndNotify(1, selectedNotes, followingNotes);
        }

        // 7 and 9 (and 8 to be more similar to division and multiply for left side) shortens/lengthens the note (by one beat, on the right side)
        if (Keyboard.current.numpad7Key.wasReleasedThisFrame
            || Keyboard.current.numpad8Key.wasReleasedThisFrame)
        {
            extendNotesAction.ExtendNotesRightAndNotify(-1, selectedNotes, followingNotes);
        }
        if (Keyboard.current.numpad9Key.wasReleasedThisFrame)
        {
            extendNotesAction.ExtendNotesRightAndNotify(1, selectedNotes, followingNotes);
        }

        // division and multiplication shortens/lengthens the note (by one beat, on the left side)
        if (Keyboard.current.numpadDivideKey.wasReleasedThisFrame)
        {
            extendNotesAction.ExtendNotesLeftAndNotify(-1, selectedNotes);
        }
        if (Keyboard.current.numpadMultiplyKey.wasReleasedThisFrame)
        {
            extendNotesAction.ExtendNotesLeftAndNotify(1, selectedNotes);
        }
        
        // Minus sign moves a note up a half-tone (due to the key's physical location, this makes sense)
        // Plus sign moves a note down a half-tone
        if (Keyboard.current.numpadMinusKey.wasReleasedThisFrame)
        {
            moveNotesAction.MoveNotesVerticalAndNotify(1, selectedNotes, followingNotes);
        }
        if (Keyboard.current.numpadPlusKey.wasReleasedThisFrame)
        {
            moveNotesAction.MoveNotesVerticalAndNotify(-1, selectedNotes, followingNotes);
        }

        // 5 plays the current selected note
        if (Keyboard.current.numpad5Key.wasReleasedThisFrame)
        {
            PlayAudioInRangeOfNotes(selectedNotes);
        }

        // scroll left with h, scroll right with j
        if (Keyboard.current.hKey.wasReleasedThisFrame)
        {
            noteArea.ScrollHorizontal(-1);
        }
        if (Keyboard.current.jKey.wasReleasedThisFrame)
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
    
    private void StartEditingNoteText()
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
}
