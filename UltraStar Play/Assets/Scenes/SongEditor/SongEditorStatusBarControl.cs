using System.Collections.Generic;
using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorStatusBarControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = R.UxmlNames.statusBarSongInfoLabel)]
    private Label statusBarSongInfoLabel;

    [Inject(UxmlName = R.UxmlNames.statusBarPositionInfoLabel)]
    private Label statusBarPositionInfoLabel;

    [Inject(UxmlName = R.UxmlNames.statusBarControlHintLabel)]
    private Label statusBarControlHintLabel;

    [Inject(UxmlName = R.UxmlNames.videoArea)]
    private VisualElement videoArea;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongEditorSelectionControl selectionControl;

    [Inject]
    private UltraStarPlayInputManager inputManager;

    [Inject]
    private GameObject gameObject;

    [Inject]
    private Settings settings;
    
    private EditorNoteControl editorNoteControlUnderPointer;
    private bool isPointerOverVideoArea;
    private bool IsPointerOverVideoArea
    {
        get
        {
            return isPointerOverVideoArea;
        }
        set
        {
            isPointerOverVideoArea = value;
            UpdateStatusBarControlHint();
        }
    }

    private EKeyboardModifier lastKeyboardModifier;

    public void OnInjectionFinished()
    {
        statusBarSongInfoLabel.text = $"{songMeta.Artist} - {songMeta.Title}";
        statusBarPositionInfoLabel.text = "";

        songAudioPlayer.PositionEventStream
            .Subscribe(millis =>
            {
                statusBarPositionInfoLabel.text = TimeUtils.GetMinutesAndSecondsDurationString(millis);
            });

        videoArea.RegisterCallback<PointerEnterEvent>(evt => IsPointerOverVideoArea = true, TrickleDown.TrickleDown);
        videoArea.RegisterCallback<PointerLeaveEvent>(evt => IsPointerOverVideoArea = false, TrickleDown.TrickleDown);

        selectionControl.NoteSelectionChangeEventStream.Subscribe(_ => UpdateStatusBarControlHint());

        InputManager.GetInputAction(R.InputActions.usplay_anyKeyboardModifierPressedOrReleased).PerformedAsObservable()
            .Subscribe(_ => UpdateStatusBarControlHint());

        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.ShowControlHints)
            .Subscribe(_ => UpdateStatusBarControlHint());

        UpdateStatusBarControlHint();
    }

    private void UpdateStatusBarControlHint()
    {
        statusBarControlHintLabel.text = GetStatusBarControlHint();
    }

    private string GetStatusBarControlHint()
    {
        if (inputManager.InputDeviceEnum != EInputDevice.KeyboardAndMouse)
        {
            return "";
        }

        if (!settings.SongEditorSettings.ShowControlHints)
        {
            return "Control hints disabled";
        }

        if (isPointerOverVideoArea)
        {
            return "Drag to change VideoGap";
        }

        List<Note> selectedNotes = selectionControl.GetSelectedNotes();
        if (editorNoteControlUnderPointer != null)
        {
            return GetPointerBasedNoteManipulationControlHint(selectedNotes);
        }

        string defaultControlHint = "Space to play / pause | Tab to select next note | Shift+Tab to select previous note";

        if (selectedNotes.Count > 0)
        {
            string keyboardBasedNoteManipulationControlHint = GetKeyboardBasedNoteManipulationControlHint();
            if (!keyboardBasedNoteManipulationControlHint.IsNullOrEmpty())
            {
                return keyboardBasedNoteManipulationControlHint;
            }

            return defaultControlHint + " | Ctrl, Shift, Alt for more actions";
        }

        return defaultControlHint;
    }

    private string GetKeyboardBasedNoteManipulationControlHint()
    {
        EKeyboardModifier currentKeyboardModifier = InputUtils.GetCurrentKeyboardModifier();
        return currentKeyboardModifier switch
        {
            EKeyboardModifier.Shift => "Arrow keys to move notes",
            EKeyboardModifier.Ctrl => "Arrow keys to move left side of notes | Space to play notes",
            EKeyboardModifier.Alt => "Arrow keys to move right side of notes",
            EKeyboardModifier.CtrlShift => "Arrow keys to move notes one octave",
            _ => ""
        };
    }

    private string GetPointerBasedNoteManipulationControlHint(List<Note> selectedNotes)
    {
        if (editorNoteControlUnderPointer.IsPointerOverCenter)
        {
            return "Drag to move notes";
        }
        else if (editorNoteControlUnderPointer.IsPointerOverLeftHandle
                 || editorNoteControlUnderPointer.IsPointerOverRightHandle)
        {
            if (InputUtils.IsKeyboardShiftPressed()
                || selectedNotes.Count <= 1
                || !selectedNotes.Contains(editorNoteControlUnderPointer.Note))
            {
                if (editorNoteControlUnderPointer.IsPointerOverLeftHandle)
                {
                    return "Drag to move left side of notes";
                }
                else
                {
                    return "Drag to move right side of notes";
                }
            }
            return "Drag to stretch notes";
        }
        return "";
    }

    public void OnPointerOverNoteControl(EditorNoteControl editorNoteControl)
    {
        editorNoteControlUnderPointer = editorNoteControl;
        UpdateStatusBarControlHint();
    }

    public void OnPointerExitNoteControl(EditorNoteControl editorNoteControl)
    {
        if (editorNoteControlUnderPointer == editorNoteControl)
        {
            editorNoteControlUnderPointer = null;
            UpdateStatusBarControlHint();
        }
    }
}
