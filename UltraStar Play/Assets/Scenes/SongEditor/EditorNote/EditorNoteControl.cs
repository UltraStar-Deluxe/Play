using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using PrimeInputActions;
using UnityEngine.UIElements;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

#pragma warning disable CS0649

public class EditorNoteControl : INeedInjection, IInjectionFinishedListener
{
    public static readonly IComparer<EditorNoteControl> comparerByStartBeat = new EditorUiNoteComparerByStartBeat();

    private static readonly float maxFontSize = 14;
    private static readonly double handleWidthInPercent = 0.25;

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    public VisualElement VisualElement { get; private set; }

    [Inject(UxmlName = R.UxmlNames.goldenNoteIndicator)]
    private VisualElement goldenNoteIndicator;

    [Inject(UxmlName = R.UxmlNames.noteImage)]
    private VisualElement backgroundImage;

    [Inject(UxmlName = R.UxmlNames.selectionIndicator)]
    private VisualElement selectionIndicator;

    [Inject(UxmlName = R.UxmlNames.rightHandle)]
    private VisualElement rightHandle;

    [Inject(UxmlName = R.UxmlNames.leftHandle)]
    private VisualElement leftHandle;

    [Inject(UxmlName = R.UxmlNames.lyricsLabel)]
    private Label lyricsLabel;

    [Inject(UxmlName = R.UxmlNames.pitchLabel)]
    private Label pitchLabel;

    // private ShowWhiteSpaceText uiText;

    // private NoteAreaDragHandler noteAreaDragHandler;
    
    [Inject]
    private SongMeta songMeta;

    [Inject]
    private MidiManager midiManager;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private Injector injector;

    [Inject]
    private SongEditorSelectionControl selectionControl;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private CursorManager cursorManager;

    [Inject]
    private NoteAreaControl noteAreaControl;

    [Inject]
    private UiManager uiManager;

    [Inject]
    public Note Note { get; private set; }

    private Vector2 pointerDownPosition;

    private EditorNoteLyricsInputField activeLyricsInputField;

    private bool isPlayingMidiSound;

    public bool IsPointerOver { get; private set; }
    public bool IsPointerOverRightHandle { get; private set; }
    public bool IsPointerOverLeftHandle { get; private set; }
    public bool IsPointerOverCenter { get; private set; }

    private float lastClickTime;

    private readonly List<IDisposable> disposables = new List<IDisposable>();

    public void OnInjectionFinished()
    {
        UpdateHandles();
        disposables.Add(InputManager.GetInputAction(R.InputActions.songEditor_anyKeyboardKey).PerformedAsObservable()
            .Subscribe(_ => UpdateHandles()));
        disposables.Add(noteAreaControl.ViewportEventStream
            .Subscribe(_ => UpdateFontSize()));
        disposables.Add(uiManager.MousePositionChangeEventStream
            .Subscribe(_ => OnPointerMove()));

        VisualElement.RegisterCallback<PointerDownEvent>(evt => OnPointerDown(evt), TrickleDown.TrickleDown);
        VisualElement.RegisterCallback<PointerUpEvent>(evt => OnPointerUp(evt), TrickleDown.TrickleDown);
        VisualElement.RegisterCallback<PointerEnterEvent>(evt => OnPointerEnter(evt), TrickleDown.TrickleDown);
        VisualElement.RegisterCallback<PointerLeaveEvent>(evt => OnPointerExit(evt), TrickleDown.TrickleDown);
        VisualElement.RegisterCallback<PointerMoveEvent>(evt => OnPointerMove(), TrickleDown.TrickleDown);

        SyncWithNote();
    }

    public void SyncWithNote()
    {
        bool isSelected = selectionControl.IsSelected(Note);
        goldenNoteIndicator.SetVisibleByDisplay(Note.IsGolden);
        SetLyrics(Note.Text);
        SetSelected(isSelected);
        pitchLabel.text = MidiUtils.GetAbsoluteName(Note.MidiNote);
        if (Note.Sentence != null && Note.Sentence.Voice != null)
        {
            Color color = songEditorSceneControl.GetColorForVoice(Note.Sentence.Voice);
            SetColor(color);
        }
        UpdateFontSize();
    }

    private void OnPointerMove()
    {
        if (IsPointerOver)
        {
            OnPointerOver();
        }

        UpdateHandles();
    }

    private void UpdateFontSize()
    {
        // float fontSize = noteArea.HeightForSingleNote * 200;
        // fontSize = Mathf.Min(fontSize, maxFontSize);
        // lyricsLabel.style.fontSize = new StyleLength(new Length(fontSize, LengthUnit.Pixel));
    }

    private void OnPointerOver()
    {
        // Vector3 mousePosition = Input.mousePosition;
        // Vector2 localPoint = RectTransform.InverseTransformPoint(mousePosition);
        // float width = RectTransform.rect.width;
        // double xPercent = (localPoint.x + (width / 2)) / width;
        // if (xPercent < handleWidthInPercent)
        // {
        //     OnPointerOverLeftHandle();
        // }
        // else if (xPercent > (1 - handleWidthInPercent))
        // {
        //     OnPointerOverRightHandle();
        // }
        // else
        // {
        //     OnPointerOverCenter();
        // }
        //
        // UpdateHandles();
    }

    private void OnPointerOverCenter()
    {
        IsPointerOverCenter = true;
        IsPointerOverLeftHandle = false;
        IsPointerOverRightHandle = false;
        SetCursorForGestureOrMusicNoteCursor(ECursor.Grab);
    }

    private void OnPointerOverRightHandle()
    {
        IsPointerOverCenter = false;
        IsPointerOverLeftHandle = false;
        IsPointerOverRightHandle = true;
        SetCursorForGestureOrMusicNoteCursor(ECursor.ArrowsLeftRight);
    }

    private void OnPointerOverLeftHandle()
    {
        IsPointerOverCenter = false;
        IsPointerOverLeftHandle = true;
        IsPointerOverRightHandle = false;
        SetCursorForGestureOrMusicNoteCursor(ECursor.ArrowsLeftRight);
    }

    private void SetCursorForGestureOrMusicNoteCursor(ECursor cursor)
    {
        if (InputUtils.IsKeyboardControlPressed())
        {
            // LeftControl is used to play midi sound, indicate this via a custom cursor.
            cursorManager.SetCursorMusicNote();
            return;
        }

        cursorManager.SetCursor(cursor);
    }

    private void UpdateHandles()
    {
        bool isSelected = (selectionControl != null) && selectionControl.IsSelected(Note);
        bool isLeftHandleVisible = IsPointerOverLeftHandle
            || (isSelected && (InputUtils.IsKeyboardControlPressed() || InputUtils.IsKeyboardShiftPressed()));
        leftHandle.SetVisibleByDisplay(isLeftHandleVisible);

        bool isRightHandleVisible = IsPointerOverRightHandle
            || (isSelected && (InputUtils.IsKeyboardAltPressed() || InputUtils.IsKeyboardShiftPressed()));
        rightHandle.SetVisibleByDisplay(isRightHandleVisible);
    }

    public bool IsPositionOverLeftHandle(Vector2 position)
    {
        // Vector2 localPoint = RectTransform.InverseTransformPoint(position);
        // float width = RectTransform.rect.width;
        // double xPercent = (localPoint.x + (width / 2)) / width;
        // return (xPercent < handleWidthInPercent);r
        return false;
    }

    public bool IsPositionOverRightHandle(Vector2 position)
    {
        // Vector2 localPoint = RectTransform.InverseTransformPoint(position);
        // float width = RectTransform.rect.width;
        // double xPercent = (localPoint.x + (width / 2)) / width;
        // return (xPercent > (1 - handleWidthInPercent));
        return false;
    }

    public void OnPointerClick(IPointerEvent ped)
    {
        // Ignore any drag motion. Dragging is used to select notes.
        float dragDistance = Vector2.Distance(pointerDownPosition, ped.position);
        bool isDrag = dragDistance > 5f;
        if (isDrag)
        {
            return;
        }

        // Only listen to left mouse button. Right mouse button is for context menu.
        if (ped.button != 0
            && Touch.activeTouches.Count == 0)
        {
            return;
        }

        // Check double click to edit lyrics
        bool isDoubleClick = Time.time - lastClickTime < InputUtils.DoubleClickThresholdInSeconds;
        lastClickTime = Time.time;
        if (isDoubleClick)
        {
            StartEditingNoteText();
            return;
        }

        // Select / deselect notes via Shift.
        if (InputUtils.IsKeyboardShiftPressed())
        {
            if (selectionControl.IsSelected(Note))
            {
                selectionControl.RemoveFromSelection(this);
            }
            else
            {
                selectionControl.AddToSelection(this);
            }
        }
        else if (!InputUtils.IsKeyboardControlPressed())
        {
            // Move the playback position to the start of the note
            double positionInSongInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, Note.StartBeat);
            songAudioPlayer.PositionInSongInMillis = positionInSongInMillis;
        }
    }

    public void SetLyrics(string newText)
    {
        switch (Note.Type)
        {
            case ENoteType.Freestyle:
                lyricsLabel.text = $"<i><b><color=#c00000>{newText}</color></b></i>";
                break;
            case ENoteType.Golden:
                lyricsLabel.text = $"<b>{newText}</b>";
                break;
            case ENoteType.Rap:
            case ENoteType.RapGolden:
                lyricsLabel.text = $"<i><b><color=#ffa500ff>{newText}</color></b></i>";
                break;
            default:
                lyricsLabel.text = newText;
                break;
        }
    }

    public void SetColor(Color color)
    {
        backgroundImage.style.backgroundColor = color;
    }

    public void SetSelected(bool isSelected)
    {
        selectionIndicator.SetVisibleByDisplay(isSelected);
    }

    public void StartEditingNoteText()
    {
        // if (activeLyricsInputField != null)
        // {
        //     return;
        // }
        //
        // activeLyricsInputField = Instantiate(lyricsInputFieldPrefab, transform);
        // injector.Inject(activeLyricsInputField);
        // activeLyricsInputField.Init(this, Note.Text);
        //
        // // Set min width of input field
        // RectTransform inputFieldRectTransform = activeLyricsInputField.GetComponent<RectTransform>();
        // Canvas canvas = CanvasUtils.FindCanvas();
        // float canvasWidth = canvas.GetComponent<RectTransform>().rect.width;
        // if ((inputFieldRectTransform.rect.width / canvasWidth) < 0.1)
        // {
        //     inputFieldRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, canvasWidth * 0.1f);
        // }
    }

    public void OnPointerEnter(IPointerEvent eventData)
    {
        IsPointerOver = true;
    }

    public void OnPointerExit(IPointerEvent eventData)
    {
        IsPointerOver = false;
        IsPointerOverCenter = false;
        IsPointerOverLeftHandle = false;
        IsPointerOverRightHandle = false;
        UpdateHandles();
        cursorManager.SetDefaultCursor();
    }

    public void OnPointerDown(IPointerEvent eventData)
    {
        pointerDownPosition = eventData.position;
        // Play midi sound via Ctrl
        if (!isPlayingMidiSound && InputUtils.IsKeyboardControlPressed())
        {
            isPlayingMidiSound = true;
            midiManager.PlayMidiNote(Note.MidiNote);
        }
    }

    public void OnPointerUp(IPointerEvent eventData)
    {
        if (isPlayingMidiSound)
        {
            midiManager.StopMidiNote(Note.MidiNote);
            isPlayingMidiSound = false;
        }
        OnPointerClick(eventData);
    }

    private class EditorUiNoteComparerByStartBeat : IComparer<EditorNoteControl>
    {
        public int Compare(EditorNoteControl x, EditorNoteControl y)
        {
            return Note.comparerByStartBeat.Compare(x.Note, y.Note);
        }
    }

    public void Dispose()
    {
        disposables.ForEach(it => it.Dispose());
        disposables.Clear();
        VisualElement.RemoveFromHierarchy();
    }

    // public void OnBeginDrag(PointerEventData eventData)
    // {
    //     noteAreaDragHandler.OnBeginDrag(eventData);
    // }
    //
    // public void OnDrag(PointerEventData eventData)
    // {
    //     noteAreaDragHandler.OnDrag(eventData);
    // }
    //
    // public void OnEndDrag(PointerEventData eventData)
    // {
    //     noteAreaDragHandler.OnEndDrag(eventData);
    // }
}
