using System;
using System.Collections.Generic;
using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

#pragma warning disable CS0649

public class EditorNoteControl : INeedInjection, IInjectionFinishedListener
{
    public static readonly IComparer<EditorNoteControl> comparerByStartBeat = new EditorUiNoteComparerByStartBeat();

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

    [Inject(UxmlName = R.UxmlNames.editLyricsPopup)]
    private VisualElement editLyricsPopup;

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
    private UIDocument uiDocument;

    [Inject]
    public Note Note { get; private set; }

    [Inject]
    private SongEditorStatusBarControl statusBarControl;

    private Vector2 pointerDownPosition;

    private EditorNoteLyricsInputControl lyricsInputControl;

    private bool isPlayingMidiSound;

    public bool IsPointerOver { get; private set; }
    public bool IsPointerOverRightHandle { get; private set; }
    public bool IsPointerOverLeftHandle { get; private set; }
    public bool IsPointerOverCenter { get; private set; }

    private float lastClickTime;

    private readonly List<IDisposable> disposables = new();

    private EditorNoteContextMenuControl contextMenuControl;

    public void OnInjectionFinished()
    {
        UpdateHandles();
        disposables.Add(InputManager.GetInputAction(R.InputActions.songEditor_anyKeyboardKey).PerformedAsObservable()
            .Subscribe(_ => UpdateHandles()));

        VisualElement.RegisterCallback<PointerDownEvent>(evt => OnPointerDown(evt), TrickleDown.TrickleDown);
        VisualElement.RegisterCallback<PointerUpEvent>(evt => OnPointerUp(evt), TrickleDown.TrickleDown);
        VisualElement.RegisterCallback<PointerEnterEvent>(evt => OnPointerEnter(), TrickleDown.TrickleDown);
        VisualElement.RegisterCallback<PointerLeaveEvent>(evt => OnPointerExit(), TrickleDown.TrickleDown);
        VisualElement.RegisterCallback<PointerMoveEvent>(evt => OnPointerMove(evt), TrickleDown.TrickleDown);

        contextMenuControl = injector
            .WithRootVisualElement(VisualElement)
            .WithBindingForInstance(this)
            .CreateAndInject<EditorNoteContextMenuControl>();
        disposables.Add(contextMenuControl);

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
    }

    private void OnPointerMove(IPointerEvent evt)
    {
        Vector2 localPoint = evt.localPosition;
        float width = VisualElement.worldBound.width;
        double xPercent = localPoint.x / width;
        if (xPercent < handleWidthInPercent)
        {
            OnPointerOverLeftHandle();
        }
        else if (xPercent > (1 - handleWidthInPercent))
        {
            OnPointerOverRightHandle();
        }
        else
        {
            OnPointerOverCenter();
        }

        UpdateHandles();
    }

    private void OnPointerOverCenter()
    {
        IsPointerOverCenter = true;
        IsPointerOverLeftHandle = false;
        IsPointerOverRightHandle = false;
        SetCursorForGestureOrMusicNoteCursor(ECursor.Grab);
        statusBarControl.OnPointerOverNoteControl(this);
    }

    private void OnPointerOverRightHandle()
    {
        IsPointerOverCenter = false;
        IsPointerOverLeftHandle = false;
        IsPointerOverRightHandle = true;
        SetCursorForGestureOrMusicNoteCursor(ECursor.ArrowsLeftRight);
        statusBarControl.OnPointerOverNoteControl(this);
    }

    private void OnPointerOverLeftHandle()
    {
        IsPointerOverCenter = false;
        IsPointerOverLeftHandle = true;
        IsPointerOverRightHandle = false;
        SetCursorForGestureOrMusicNoteCursor(ECursor.ArrowsLeftRight);
        statusBarControl.OnPointerOverNoteControl(this);
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

    public bool IsPositionOverLeftHandle(Vector2 screenPosition)
    {
        Vector2 localPoint = screenPosition - VisualElement.worldBound.position;
        float width = VisualElement.worldBound.width;
        double xPercent = localPoint.x / width;
        return xPercent < handleWidthInPercent;
    }

    public bool IsPositionOverRightHandle(Vector2 screenPosition)
    {
        Vector2 localPoint = screenPosition - VisualElement.worldBound.position;
        float width = VisualElement.worldBound.width;
        double xPercent = localPoint.x / width;
        return xPercent > (1 - handleWidthInPercent);
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
            songAudioPlayer.PauseAudio();
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
        string visibleWhitespaceText = ShowWhiteSpaceText.ReplaceWhiteSpaceWithVisibleCharacters(newText);
        switch (Note.Type)
        {
            case ENoteType.Freestyle:
                lyricsLabel.text = $"<i><b><color=#c00000>{visibleWhitespaceText}</color></b></i>";
                break;
            case ENoteType.Golden:
                lyricsLabel.text = $"<b>{visibleWhitespaceText}</b>";
                break;
            case ENoteType.Rap:
            case ENoteType.RapGolden:
                lyricsLabel.text = $"<i><b><color=#ffa500ff>{visibleWhitespaceText}</color></b></i>";
                break;
            default:
                lyricsLabel.text = visibleWhitespaceText;
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
        // Position TextField
        float margin = 5;
        float width = Mathf.Max(VisualElement.worldBound.width + margin * 2, 150);
        float height = VisualElement.worldBound.height + margin * 2;
        editLyricsPopup.style.position = new StyleEnum<Position>(Position.Absolute);
        editLyricsPopup.style.left = VisualElement.worldBound.x - width / 2;
        editLyricsPopup.style.top = VisualElement.worldBound.y - margin;
        editLyricsPopup.style.width = width;
        editLyricsPopup.style.height = height;

        lyricsInputControl = injector
            .WithBindingForInstance(this)
            .CreateAndInject<EditorNoteLyricsInputControl>();
    }

    private void OnPointerEnter()
    {
        IsPointerOver = true;
        statusBarControl.OnPointerOverNoteControl(this);
    }

    private void OnPointerExit()
    {
        IsPointerOver = false;
        IsPointerOverCenter = false;
        IsPointerOverLeftHandle = false;
        IsPointerOverRightHandle = false;
        UpdateHandles();
        cursorManager.SetDefaultCursor();
        statusBarControl.OnPointerExitNoteControl(this);
    }

    private void OnPointerDown(IPointerEvent eventData)
    {
        pointerDownPosition = eventData.position;
        // Play midi sound via Ctrl
        if (!isPlayingMidiSound && InputUtils.IsKeyboardControlPressed())
        {
            isPlayingMidiSound = true;
            midiManager.PlayMidiNote(Note.MidiNote);
        }
    }

    private void OnPointerUp(IPointerEvent eventData)
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

    public bool IsEditingLyrics()
    {
        return lyricsInputControl != null
               && lyricsInputControl.IsActive();
    }

    public void SubmitAndCloseLyricsDialog()
    {
        lyricsInputControl.SubmitAndCloseLyricsDialog();
    }

    public void HideLabels()
    {
        lyricsLabel.HideByDisplay();
        pitchLabel.HideByDisplay();
    }

    public void ShowLabels()
    {
        lyricsLabel.ShowByDisplay();
        pitchLabel.ShowByDisplay();
    }
}
