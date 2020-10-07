using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.EventSystems;

#pragma warning disable CS0649

public class EditorUiNote : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    public static readonly IComparer<EditorUiNote> comparerByStartBeat = new EditorUiNoteComparerByStartBeat();

    private static readonly double handleWidthInPercent = 0.25;

    [InjectedInInspector]
    public EditorNoteLyricsInputField lyricsInputFieldPrefab;

    [InjectedInInspector]
    public Image goldenNoteImageOverlay;

    [InjectedInInspector]
    public Image backgroundImage;

    [InjectedInInspector]
    public RectTransform selectionIndicator;

    [InjectedInInspector]
    public RectTransform rightHandle;

    [InjectedInInspector]
    public RectTransform leftHandle;

    [InjectedInInspector]
    public Text pitchLabel;

    [InjectedInInspector]
    public ShowWhiteSpaceText uiText;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private MidiManager midiManager;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject(searchMethod = SearchMethods.GetComponent)]
    public RectTransform RectTransform { get; private set; }

    [Inject]
    private Injector injector;

    [Inject]
    private SongEditorSelectionController selectionController;

    [Inject]
    private SongEditorSceneController songEditorSceneController;

    [Inject]
    private CursorManager cursorManager;

    private EditorNoteLyricsInputField activeLyricsInputField;

    private bool isPlayingMidiSound;

    public bool IsPointerOver { get; private set; }
    public bool IsPointerOverRightHandle { get; private set; }
    public bool IsPointerOverLeftHandle { get; private set; }
    public bool IsPointerOverCenter { get; private set; }

    public Note Note { get; private set; }

    public void Init(Note note)
    {
        this.Note = note;
        SyncWithNote();
    }

    public void SyncWithNote()
    {
        bool isSelected = selectionController.IsSelected(Note);
        goldenNoteImageOverlay.gameObject.SetActive(Note.IsGolden);
        SetLyrics(Note.Text);
        SetSelected(isSelected);
        pitchLabel.text = MidiUtils.GetAbsoluteName(Note.MidiNote);
        if (Note.Sentence != null && Note.Sentence.Voice != null)
        {
            Color color = songEditorSceneController.GetColorForVoice(Note.Sentence.Voice);
            SetColor(color);
        }
    }

    void Start()
    {
        UpdateHandles();
    }

    void Update()
    {
        if (IsPointerOver)
        {
            OnPointerOver();
        }

        if (IsKeyDownOrUp(KeyCode.LeftControl) || IsKeyDownOrUp(KeyCode.LeftAlt) || IsKeyDownOrUp(KeyCode.LeftShift))
        {
            UpdateHandles();
        }

        UpdateFontSize();
    }

    private void UpdateFontSize()
    {
        float rectTransformWidthInPt = Mathf.Floor(RectTransform.rect.width * 72 / Screen.dpi) - 2;
        uiText.FontSize = (int)Mathf.Max(2, Mathf.Min(20f, rectTransformWidthInPt));
    }

    private bool IsKeyDownOrUp(KeyCode keyCode)
    {
        return Input.GetKeyDown(keyCode) || Input.GetKeyUp(keyCode);
    }

    private void OnPointerOver()
    {
        Vector3 mousePosition = Input.mousePosition;
        Vector2 localPoint = RectTransform.InverseTransformPoint(mousePosition);
        float width = RectTransform.rect.width;
        double xPercent = (localPoint.x + (width / 2)) / width;
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
        if (Input.GetKey(KeyCode.LeftControl))
        {
            // LeftControl is used to play midi sound, indicate this via a custom cursor.
            cursorManager.SetCursorMusicNote();
            return;
        }

        cursorManager.SetCursor(cursor);
    }

    private void UpdateHandles()
    {
        bool isSelected = (selectionController != null) && selectionController.IsSelected(Note);
        bool isLeftHandleVisible = IsPointerOverLeftHandle
            || (isSelected && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftShift)));
        bool isRightHandleVisible = IsPointerOverRightHandle
            || (isSelected && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.LeftShift)));
        leftHandle.gameObject.SetActive(isLeftHandleVisible);
        rightHandle.gameObject.SetActive(isRightHandleVisible);
    }

    public bool IsPositionOverLeftHandle(Vector2 position)
    {
        Vector2 localPoint = RectTransform.InverseTransformPoint(position);
        float width = RectTransform.rect.width;
        double xPercent = (localPoint.x + (width / 2)) / width;
        return (xPercent < handleWidthInPercent);
    }

    public bool IsPositionOverRightHandle(Vector2 position)
    {
        Vector2 localPoint = RectTransform.InverseTransformPoint(position);
        float width = RectTransform.rect.width;
        double xPercent = (localPoint.x + (width / 2)) / width;
        return (xPercent > (1 - handleWidthInPercent));
    }

    public void OnPointerClick(PointerEventData ped)
    {
        // Ignore any drag motion. Dragging is used to select notes.
        float dragDistance = Vector2.Distance(ped.pressPosition, ped.position);
        bool isDrag = dragDistance > 5f;
        if (isDrag)
        {
            return;
        }

        // Only listen to left mouse button. Right mouse button is for context menu.
        if (ped.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (ped.clickCount == 1)
        {
            // Select / deselect notes via Shift.
            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (selectionController.IsSelected(Note))
                {
                    selectionController.RemoveFromSelection(this);
                }
                else
                {
                    selectionController.AddToSelection(this);
                }
            }
            else if (!Input.GetKey(KeyCode.LeftControl))
            {
                // Move the playback position to the start of the note
                double positionInSongInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, Note.StartBeat);
                songAudioPlayer.PositionInSongInMillis = positionInSongInMillis;
            }
        }
        else if (ped.clickCount == 2)
        {
            StartEditingNoteText();
        }
    }

    public void SetLyrics(string newText)
    {
        switch (Note.Type)
        {
            case ENoteType.Freestyle:
                uiText.text = $"<i><b><color=#c00000>{newText}</color></b></i>";
                break;
            case ENoteType.Golden:
                uiText.text = $"<b>{newText}</b>";
                break;
            case ENoteType.Rap:
            case ENoteType.RapGolden:
                uiText.text = $"<i><b><color=#ffa500ff>{newText}</color></b></i>";
                break;
            default:
                uiText.text = newText;
                break;
        }
    }

    public void SetColor(Color color)
    {
        backgroundImage.color = color;
    }

    public void SetSelected(bool isSelected)
    {
        selectionIndicator.gameObject.SetActive(isSelected);
    }

    public void StartEditingNoteText()
    {
        if (activeLyricsInputField != null)
        {
            return;
        }

        activeLyricsInputField = Instantiate(lyricsInputFieldPrefab, transform);
        injector.Inject(activeLyricsInputField);
        activeLyricsInputField.Init(this, Note.Text);

        // Set min width of input field
        RectTransform inputFieldRectTransform = activeLyricsInputField.GetComponent<RectTransform>();
        Canvas canvas = CanvasUtils.FindCanvas();
        float canvasWidth = canvas.GetComponent<RectTransform>().rect.width;
        if ((inputFieldRectTransform.rect.width / canvasWidth) < 0.1)
        {
            inputFieldRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, canvasWidth * 0.1f);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        IsPointerOver = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        IsPointerOver = false;
        IsPointerOverCenter = false;
        IsPointerOverLeftHandle = false;
        IsPointerOverRightHandle = false;
        UpdateHandles();
        cursorManager.SetDefaultCursor();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Play midi sound via Ctrl
        if (!isPlayingMidiSound && Input.GetKey(KeyCode.LeftControl))
        {
            isPlayingMidiSound = true;
            midiManager.PlayMidiNote(Note.MidiNote);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isPlayingMidiSound)
        {
            midiManager.StopMidiNote(Note.MidiNote);
            isPlayingMidiSound = false;
        }
    }

    private class EditorUiNoteComparerByStartBeat : IComparer<EditorUiNote>
    {
        public int Compare(EditorUiNote x, EditorUiNote y)
        {
            return Note.comparerByStartBeat.Compare(x.Note, y.Note);
        }
    }
}
