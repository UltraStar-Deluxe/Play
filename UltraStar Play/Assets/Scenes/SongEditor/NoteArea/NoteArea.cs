using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UniInject;
using UniRx;
using UnityEngine.UI;
using System.Linq;

#pragma warning disable CS0649

public class NoteArea : MonoBehaviour, INeedInjection, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    // The first midi note that is visible in the viewport
    public int ViewportY { get; private set; }
    // The number of midi notes that are visible in the viewport
    public int ViewportHeight { get; private set; }

    // The viewport left side in the song in milliseconds
    public int ViewportX { get; private set; }
    // The width of the viewport in milliseconds
    public int ViewportWidth { get; private set; }

    public int ViewportWidthInBeats
    {
        get
        {
            return MaxBeatInViewport - MinBeatInViewport;
        }
    }

    public int MinBeatInViewport { get; private set; }
    public int MaxBeatInViewport { get; private set; }

    public int MinMillisecondsInViewport { get; private set; }
    public int MaxMillisecondsInViewport { get; private set; }

    public int MinMidiNoteInViewport { get; private set; }
    public int MaxMidiNoteInViewport { get; private set; }

    public double MillisecondsPerBeat { get; private set; }
    public float HeightForSingleNote { get; private set; }

    public bool IsPointerOver { get; private set; }

    [Inject]
    private SongMeta songMeta;

    [Inject(key = "voices")]
    private List<Voice> voices;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject(searchMethod = SearchMethods.GetComponent)]
    private RectTransform rectTransform;

    [Inject]
    private SongEditorLayerManager songEditorLayerManager;

    private readonly Subject<ViewportEvent> viewportEventStream = new Subject<ViewportEvent>();
    public ISubject<ViewportEvent> ViewportEventStream
    {
        get
        {
            return viewportEventStream;
        }
    }

    void Start()
    {
        MillisecondsPerBeat = BpmUtils.MillisecondsPerBeat(songMeta);

        // Initialize Viewport
        SetViewportX(0, true);
        SetViewportY(MidiUtils.MidiNoteMin + (MidiUtils.SingableNoteRange / 4), true);
        SetViewportWidth(3000, true);
        SetViewportHeight(MidiUtils.SingableNoteRange / 2, true);

        songAudioPlayer.PositionInSongEventStream.Subscribe(SetPositionInSongInMillis);

        FitViewportVerticalToNotes();
    }

    public void FitViewportVerticalToNotes()
    {
        List<Note> notesInLayers = songEditorLayerManager.GetAllNotes();
        List<Note> notesInVoices = voices.SelectMany(voice => voice.Sentences).SelectMany(sentence => sentence.Notes).ToList();
        List<Note> notes = new List<Note>();
        notes.AddRange(notesInLayers);
        notes.AddRange(notesInVoices);
        int minMidiNoteInSong = notes.Select(note => note.MidiNote).Min();
        int maxMidiNoteInSong = notes.Select(note => note.MidiNote).Max();
        if (minMidiNoteInSong > 0 && maxMidiNoteInSong > 0)
        {
            SetViewportY(minMidiNoteInSong - 1);
            SetViewportHeight(maxMidiNoteInSong - minMidiNoteInSong + 1);
        }
    }

    public void FitViewportHorizontalToSentence(Sentence currentSentence)
    {
        double minPositionInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, currentSentence.MinBeat);
        double maxPositionInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, currentSentence.MaxBeat);
        SetViewportX((int)Math.Floor(minPositionInMillis));
        SetViewportWidth((int)Math.Ceiling(maxPositionInMillis - minPositionInMillis));
    }

    public void SetPositionInSongInMillis(double positionInSongInMillis)
    {
        float viewportAutomaticScrollingLeft = ViewportX + ViewportWidth * 0.1f;
        float viewportAutomaticScrollingRight = ViewportX + ViewportWidth * 0.9f;
        if (positionInSongInMillis < ViewportX || positionInSongInMillis > (ViewportX + ViewportWidth))
        {
            // Center viewport to position in song
            double newViewportX = positionInSongInMillis - ViewportWidth / 2;
            SetViewportX((int)newViewportX);
        }
        else if (positionInSongInMillis < viewportAutomaticScrollingLeft)
        {
            // Scroll left to new position
            double newViewportX = ViewportX - (viewportAutomaticScrollingLeft - positionInSongInMillis);
            SetViewportX((int)newViewportX);
        }
        else if (positionInSongInMillis > viewportAutomaticScrollingRight)
        {
            // Scroll right to new position
            double newViewportX = ViewportX + (positionInSongInMillis - viewportAutomaticScrollingRight);
            SetViewportX((int)newViewportX);
        }
    }

    public bool IsNoteVisible(Note note)
    {
        // Check y axis, which is the midi note
        bool isMidiNoteOk = (note.MidiNote >= MinMidiNoteInViewport)
                         && (note.MidiNote <= MaxMidiNoteInViewport);
        if (!isMidiNoteOk)
        {
            return false;
        }

        // Check x axis, which is the position in the song
        double startPosInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, note.StartBeat);
        double endPosInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, note.EndBeat);
        bool isMillisecondsOk = (startPosInMillis >= MinMillisecondsInViewport)
                             && (endPosInMillis <= MaxMillisecondsInViewport);
        return isMillisecondsOk;
    }

    public double GetVerticalPositionForMidiNote(int midiNote)
    {
        int indexInViewport = midiNote - ViewportY;
        return (double)indexInViewport / ViewportHeight;
    }

    public double GetHorizontalPositionForMillis(double positionInSongInMillis)
    {
        return (positionInSongInMillis - ViewportX) / ViewportWidth;
    }

    public double GetHorizontalPositionForBeat(int beat)
    {
        double positionInSongInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, beat);
        return GetHorizontalPositionForMillis(positionInSongInMillis);
    }

    public bool IsInViewport(Note note)
    {
        return note.StartBeat <= MaxBeatInViewport && note.EndBeat >= MinBeatInViewport
            && note.MidiNote <= MaxMidiNoteInViewport && note.MidiNote >= MinMidiNoteInViewport;
    }

    public bool IsInViewport(Sentence sentence)
    {
        return sentence.MinBeat <= MaxBeatInViewport && Math.Max(sentence.MaxBeat, sentence.LinebreakBeat) >= MinBeatInViewport;
    }

    public void ScrollHorizontal(int direction)
    {
        int newViewportX = ViewportX + direction * (int)(ViewportWidth * 0.2);
        SetViewportX(newViewportX);
    }

    public void ZoomHorizontal(int direction)
    {
        double viewportChangeInPercent = 0.25;

        Vector2 mouseLocalPosition = rectTransform.InverseTransformPoint(Input.mousePosition);
        float width = rectTransform.rect.width;
        double xPercent = (mouseLocalPosition.x + (width / 2)) / width;

        double zoomFactor = (direction > 0) ? (1 - viewportChangeInPercent) : (1 + viewportChangeInPercent);
        int newViewportWidth = (int)(ViewportWidth * zoomFactor);

        int viewportChange = ViewportWidth - newViewportWidth;
        int viewportChangeLeftSide = (int)(viewportChange * xPercent);
        int newViewportX = ViewportX + viewportChangeLeftSide;

        int oldViewportWidth = ViewportWidth;
        SetViewportWidth(newViewportWidth);
        if (oldViewportWidth != ViewportWidth)
        {
            SetViewportX(newViewportX);
        }
    }

    public void ScrollVertical(int direction)
    {
        int newViewportY = ViewportY + direction;
        SetViewportY(newViewportY);
    }

    public void ZoomVertical(int direction)
    {
        double viewportChangeInPercent = 0.25;

        Vector2 mouseLocalPosition = rectTransform.InverseTransformPoint(Input.mousePosition);
        float height = rectTransform.rect.height;
        double yPercent = (mouseLocalPosition.y + (height / 2)) / height;

        double zoomFactor = (direction > 0) ? (1 - viewportChangeInPercent) : (1 + viewportChangeInPercent);
        int newViewportHeight = (int)(ViewportHeight * zoomFactor);

        int viewportChange = ViewportHeight - newViewportHeight;
        int viewportChangeBottomSide = (int)(viewportChange * yPercent);
        int newViewportY = ViewportY + viewportChangeBottomSide;

        int oldViewportHeight = ViewportHeight;
        SetViewportHeight(newViewportHeight);
        if (oldViewportHeight != ViewportHeight)
        {
            SetViewportY(newViewportY);
        }
    }

    public void SetViewportX(int newViewportX, bool force = false)
    {
        if (newViewportX < 0)
        {
            newViewportX = 0;
        }
        if (newViewportX >= songAudioPlayer.DurationOfSongInMillis)
        {
            newViewportX = (int)songAudioPlayer.DurationOfSongInMillis;
        }

        if (force || newViewportX != ViewportX)
        {
            ViewportX = newViewportX;
            MinMillisecondsInViewport = ViewportX;
            MaxMillisecondsInViewport = ViewportX + ViewportWidth;
            MinBeatInViewport = (int)Math.Floor(BpmUtils.MillisecondInSongToBeat(songMeta, MinMillisecondsInViewport));
            MaxBeatInViewport = (int)Math.Ceiling(BpmUtils.MillisecondInSongToBeat(songMeta, MaxMillisecondsInViewport));
            FireViewportChangedEvent();
        }
    }

    public void SetViewportY(int newViewportY, bool force = false)
    {
        if (newViewportY < 0)
        {
            newViewportY = 0;
        }
        if (newViewportY > 127)
        {
            newViewportY = 127;
        }

        if (force || newViewportY != ViewportY)
        {
            ViewportY = newViewportY;
            MinMidiNoteInViewport = ViewportY;
            MaxMidiNoteInViewport = ViewportY + ViewportHeight;
            FireViewportChangedEvent();
        }
    }

    public void SetViewportHeight(int newViewportHeight, bool force = false)
    {
        if (newViewportHeight < 12)
        {
            newViewportHeight = 12;
        }
        if (newViewportHeight > 48)
        {
            newViewportHeight = 48;
        }

        if (force || newViewportHeight != ViewportHeight)
        {
            ViewportHeight = newViewportHeight;
            MaxMidiNoteInViewport = ViewportY + ViewportHeight;
            HeightForSingleNote = 1f / ViewportHeight;
            FireViewportChangedEvent();
        }
    }

    public void SetViewportWidth(int newViewportWidth, bool force = false)
    {
        if (newViewportWidth < 1000)
        {
            newViewportWidth = 1000;
        }
        if (newViewportWidth >= songAudioPlayer.DurationOfSongInMillis)
        {
            newViewportWidth = (int)songAudioPlayer.DurationOfSongInMillis;
        }

        if (force || newViewportWidth != ViewportWidth)
        {
            ViewportWidth = newViewportWidth;
            MaxMillisecondsInViewport = ViewportX + ViewportWidth;
            MaxBeatInViewport = (int)Math.Ceiling(BpmUtils.MillisecondInSongToBeat(songMeta, MaxMillisecondsInViewport));
            FireViewportChangedEvent();
        }
    }

    private void FireViewportChangedEvent()
    {
        ViewportEvent viewportEvent = new ViewportEvent(ViewportX, ViewportY, ViewportWidth, ViewportHeight);
        viewportEventStream.OnNext(viewportEvent);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        IsPointerOver = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        IsPointerOver = true;
    }

    public void OnPointerClick(PointerEventData ped)
    {
        // Only listen to left mouse button. Right mouse button is for context menu.
        if (ped.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        // Ignore any drag motion. Dragging is used to select notes.
        float dragDistance = Vector2.Distance(ped.pressPosition, ped.position);
        bool isDrag = dragDistance > 5f;
        if (isDrag)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,
                                                                ped.position,
                                                                ped.pressEventCamera,
                                                                out Vector2 localPoint);

        float rectWidth = rectTransform.rect.width;
        double xPercent = (localPoint.x + (rectWidth / 2)) / rectWidth;
        double positionInSongInMillis = ViewportX + (ViewportWidth * xPercent);
        songAudioPlayer.PositionInSongInMillis = positionInSongInMillis;
    }
}
