﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UniInject;
using UniRx;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

#pragma warning disable CS0649

public class NoteArea : MonoBehaviour, INeedInjection, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private const float ViewportAutomaticScrollingBoarderPercent = 0.0333f;
    private const float ViewportAutomaticScrollingJumpPercent = 0.333f;
    private const int DefaultViewportWidthInMillis = 8000;

    public const int ViewportMinHeight = 12;
    // A piano has 88 keys.
    public const int ViewportMaxHeight = 88;

    // 1000 milliseconds
    public const int ViewportMinWidth = 1000;
    public int ViewportMaxWidth => (int)songAudioPlayer.DurationOfSongInMillis;

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

    [Inject]
    private SongEditorSceneController songEditorSceneController;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject(searchMethod = SearchMethods.GetComponent)]
    private RectTransform rectTransform;

    [Inject]
    private SongEditorLayerManager songEditorLayerManager;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    private readonly Subject<ViewportEvent> viewportEventStream = new Subject<ViewportEvent>();
    public ISubject<ViewportEvent> ViewportEventStream
    {
        get
        {
            return viewportEventStream;
        }
    }

    private float lastClickTime;

    void Start()
    {
        MillisecondsPerBeat = BpmUtils.MillisecondsPerBeat(songMeta);

        if (songAudioPlayer.PositionInSongInMillis == 0)
        {
            songAudioPlayer.PositionInSongInMillis = songMeta.Gap - DefaultViewportWidthInMillis * 0.25f;
        }
        InitializeViewport();

        songAudioPlayer.PositionInSongEventStream.Subscribe(SetPositionInSongInMillis);

        songMetaChangeEventStream.Subscribe(OnSongMetaChanged);
    }

    private void OnSongMetaChanged(SongMetaChangeEvent changeEvent)
    {
        if (changeEvent is BpmChangeEvent || changeEvent is LoadedMementoEvent)
        {
            SetViewportHorizontal(ViewportX, ViewportWidth);
        }
    }

    public void FitViewportVerticalToNotes()
    {
        List<Note> notesInLayers = songEditorLayerManager.GetAllNotes();
        List<Note> notesInVoices = songMeta.GetVoices().SelectMany(voice => voice.Sentences)
            .SelectMany(sentence => sentence.Notes).ToList();
        List<Note> notes = new List<Note>();
        notes.AddRange(notesInLayers);
        notes.AddRange(notesInVoices);
        int minMidiNoteInSong = notes.Select(note => note.MidiNote).Min();
        int maxMidiNoteInSong = notes.Select(note => note.MidiNote).Max();
        if (minMidiNoteInSong > 0 && maxMidiNoteInSong > 0)
        {
            int newViewportY = minMidiNoteInSong - 1;
            int newViewportHeight = (maxMidiNoteInSong - minMidiNoteInSong) + 2;
            SetViewportVertical(newViewportY, newViewportHeight);
        }
    }

    public void FitViewportHorizontal(int minBeat, int maxBeat)
    {
        maxBeat = NumberUtils.Limit(maxBeat, 0, maxBeat);
        minBeat = NumberUtils.Limit(minBeat, 0, maxBeat);
        double minPositionInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, minBeat);
        double maxPositionInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, maxBeat);
        int newViewportX = (int)Math.Floor(minPositionInMillis);
        int newViewportWidth = (int)Math.Ceiling(maxPositionInMillis - minPositionInMillis);
        SetViewportHorizontal(newViewportX, newViewportWidth);
    }

    public void SetPositionInSongInMillis(double positionInSongInMillis)
    {
        float viewportAutomaticScrollingLeft = ViewportX + ViewportWidth * ViewportAutomaticScrollingBoarderPercent;
        float viewportAutomaticScrollingRight = ViewportX + ViewportWidth * (1 - ViewportAutomaticScrollingBoarderPercent);
        if (positionInSongInMillis < ViewportX || positionInSongInMillis > (ViewportX + ViewportWidth))
        {
            // Center viewport to position in song
            double newViewportX = positionInSongInMillis - ViewportWidth * 0.5;
            SetViewportX((int)newViewportX);
        }
        else if (positionInSongInMillis < viewportAutomaticScrollingLeft)
        {
            // Scroll left to new position
            double newViewportX = positionInSongInMillis - ViewportWidth * ViewportAutomaticScrollingJumpPercent;
            SetViewportX((int)newViewportX);
        }
        else if (positionInSongInMillis > viewportAutomaticScrollingRight)
        {
            // Scroll right to new position
            double newViewportX = positionInSongInMillis - ViewportWidth * (1 - ViewportAutomaticScrollingJumpPercent);
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
        if (ViewportHeight == 0)
        {
            return 0;
        }
        int indexInViewport = midiNote - ViewportY;
        return (double)indexInViewport / ViewportHeight;
    }

    public double GetHorizontalPositionForMillis(double positionInSongInMillis)
    {
        if (ViewportWidth == 0)
        {
            return 0;
        }
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
        return MinBeatInViewport <= sentence.ExtendedMaxBeat && sentence.MinBeat <= MaxBeatInViewport;
    }

    public bool IsBeatInViewport(int beat)
    {
        return MinBeatInViewport <= beat && beat <= MaxBeatInViewport;
    }

    public void ScrollHorizontal(int direction)
    {
        int newViewportX = ViewportX + direction * (int)(ViewportWidth * 0.2);
        SetViewportX(newViewportX);
    }

    public int GetHorizontalMousePositionInMillis()
    {
        Vector2 mouseLocalPosition = rectTransform.InverseTransformPoint(Input.mousePosition);
        float width = rectTransform.rect.width;
        double xPercent = (mouseLocalPosition.x + (width / 2)) / width;
        return ViewportX + (int)(xPercent * ViewportWidth);
    }

    public double GetHorizontalMousePositionInBeats()
    {
        int millis = GetHorizontalMousePositionInMillis();
        double beat = BpmUtils.MillisecondInSongToBeat(songMeta, millis);
        return beat;
    }

    public int GetVerticalMousePositionInMidiNote()
    {
        Vector2 mouseLocalPosition = rectTransform.InverseTransformPoint(Input.mousePosition);
        float height = rectTransform.rect.height;
        double yPercent = (mouseLocalPosition.y + (height / 2)) / height;
        return ViewportY + (int)Math.Round(yPercent * ViewportHeight);
    }

    public void ZoomHorizontal(int direction)
    {
        double viewportChangeInPercent = 0.25;

        Vector2 zoomPosition = Touch.activeTouches.Count == 2
                // Center position of the touches
                ? (Touch.activeTouches[0].screenPosition + Touch.activeTouches[1].screenPosition) / 2f
                // Mouse position
                : InputUtils.GetMousePosition();
        Vector2 localZoomPosition = rectTransform.InverseTransformPoint(zoomPosition);
        float width = rectTransform.rect.width;
        double xPercent = (localZoomPosition.x + (width / 2)) / width;

        double zoomFactor = (direction > 0) ? (1 - viewportChangeInPercent) : (1 + viewportChangeInPercent);
        int newViewportWidth = (int)(ViewportWidth * zoomFactor);
        newViewportWidth = NumberUtils.Limit(newViewportWidth, ViewportMinWidth, ViewportMaxWidth);
        
        // Already reached min or max zoom. 
        if (newViewportWidth == ViewportWidth)
        {
            return;
        }
        
        int viewportChange = ViewportWidth - newViewportWidth;
        int viewportChangeLeftSide = (int)(viewportChange * xPercent);
        int newViewportX = ViewportX + viewportChangeLeftSide;

        int oldViewportWidth = ViewportWidth;
        if (oldViewportWidth != newViewportWidth)
        {
            SetViewportHorizontal(newViewportX, newViewportWidth);
        }
    }

    public void ScrollVertical(int direction)
    {
        int newViewportY = ViewportY + direction;
        SetViewportY(newViewportY);
    }

    public void ZoomVertical(int direction)
    {
        if (direction < 0 && ViewportHeight >= ViewportMaxHeight
            || direction > 0 && ViewportHeight <= ViewportMinHeight)
        {
            // The max zoom limit has been reached. Ignore further attemps to zoom.
            return;
        }

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
        if (oldViewportHeight != newViewportHeight)
        {
            SetViewportVertical(newViewportY, newViewportHeight);
        }
    }

    public void SetViewportX(int newViewportX)
    {
        SetViewportXWithoutChangeEvent(newViewportX);
        FireViewportChangedEvent();
    }

    public void SetViewportY(int newViewportY)
    {
        SetViewportYWithoutChangeEvent(newViewportY);
        FireViewportChangedEvent();
    }

    public void SetViewportHeight(int newViewportHeight)
    {
        SetViewportHeightWithoutChangeEvent(newViewportHeight);
        FireViewportChangedEvent();
    }

    public void SetViewportWidth(int newViewportWidth)
    {
        SetViewportWidthWithoutChangeEvent(newViewportWidth);
        FireViewportChangedEvent();
    }

    public void SetViewportHorizontal(int newViewportX, int newViewportWidth)
    {
        SetViewportXWithoutChangeEvent(newViewportX);
        SetViewportWidthWithoutChangeEvent(newViewportWidth);
        FireViewportChangedEvent();
    }

    public void SetViewportVertical(int newViewportY, int newViewportHeight)
    {
        SetViewportYWithoutChangeEvent(newViewportY);
        SetViewportHeightWithoutChangeEvent(newViewportHeight);
        FireViewportChangedEvent();
    }

    public void SetViewport(int newViewportX, int newViewportY, int newViewportWidth, int newViewportHeight)
    {
        SetViewportXWithoutChangeEvent(newViewportX);
        SetViewportWidthWithoutChangeEvent(newViewportWidth);
        SetViewportYWithoutChangeEvent(newViewportY);
        SetViewportHeightWithoutChangeEvent(newViewportHeight);
        FireViewportChangedEvent();
    }

    private void SetViewportXWithoutChangeEvent(int newViewportX)
    {
        if (newViewportX < 0)
        {
            newViewportX = 0;
        }
        if (newViewportX >= ViewportMaxWidth)
        {
            newViewportX = ViewportMaxWidth;
        }

        ViewportX = newViewportX;
        MinMillisecondsInViewport = ViewportX;
        MaxMillisecondsInViewport = ViewportX + ViewportWidth;
        MinBeatInViewport = (int)Math.Floor(BpmUtils.MillisecondInSongToBeat(songMeta, MinMillisecondsInViewport));
        MaxBeatInViewport = (int)Math.Ceiling(BpmUtils.MillisecondInSongToBeat(songMeta, MaxMillisecondsInViewport));
    }

    private void SetViewportYWithoutChangeEvent(int newViewportY)
    {
        if (newViewportY < 0)
        {
            newViewportY = 0;
        }
        if (newViewportY > 127)
        {
            newViewportY = 127;
        }

        ViewportY = newViewportY;
        MinMidiNoteInViewport = ViewportY;
        MaxMidiNoteInViewport = ViewportY + ViewportHeight;
    }

    private void SetViewportHeightWithoutChangeEvent(int newViewportHeight)
    {
        if (newViewportHeight < ViewportMinHeight)
        {
            newViewportHeight = ViewportMinHeight;
        }
        if (newViewportHeight > ViewportMaxHeight)
        {
            newViewportHeight = ViewportMaxHeight;
        }

        ViewportHeight = newViewportHeight;
        MaxMidiNoteInViewport = ViewportY + ViewportHeight;
        HeightForSingleNote = 1f / ViewportHeight;
    }

    private void SetViewportWidthWithoutChangeEvent(int newViewportWidth)
    {
        if (newViewportWidth < ViewportMinWidth)
        {
            newViewportWidth = ViewportMinWidth;
        }
        if (newViewportWidth >= ViewportMaxWidth)
        {
            newViewportWidth = ViewportMaxWidth;
        }

        ViewportWidth = newViewportWidth;
        MaxMillisecondsInViewport = ViewportX + ViewportWidth;
        MaxBeatInViewport = (int)Math.Ceiling(BpmUtils.MillisecondInSongToBeat(songMeta, MaxMillisecondsInViewport));
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
        if (ped.button == PointerEventData.InputButton.Right)
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

        // Toggle play pause with double click / double tap
        bool isDoubleClick = Time.time - lastClickTime < InputUtils.DoubleClickThresholdInSeconds;
        lastClickTime = Time.time;
        if (isDoubleClick)
        {
            songEditorSceneController.ToggleAudioPlayPause();
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

    private void InitializeViewport()
    {
        int minMidiNote = SongMetaUtils.GetAllNotes(songMeta).Select(note => note.MidiNote).Min();
        int maxMidiNote = SongMetaUtils.GetAllNotes(songMeta).Select(note => note.MidiNote).Max();
        // 10 seconds
        int width = DefaultViewportWidthInMillis;
        // Start at the beginning
        int x = Math.Max(0, (int)songAudioPlayer.PositionInSongInMillis - 1000);
        // Full range of notes. At least one octave
        int height = Math.Max(12, maxMidiNote - minMidiNote + 2);
        // Center the notes
        int y = minMidiNote - 1;
        SetViewport(x, y, width, height);
    }

    public float MillisecondsToPixels(int millis)
    {
        Rect rect = RectTransformUtils.GetScreenCoordinates(rectTransform);
        return rect.x + rect.width * ((float)(millis - MinMillisecondsInViewport) / ViewportWidth);
    }

    public float BeatToPixels(int beat)
    {
        Rect rect = RectTransformUtils.GetScreenCoordinates(rectTransform);
        return rect.x + rect.width * ((float)(beat - MinBeatInViewport) / ViewportWidthInBeats);
    }

    public float MidiNoteToPixels(int midiNote)
    {
        Rect rect = RectTransformUtils.GetScreenCoordinates(rectTransform);
        return rect.y + rect.height * ((float)(midiNote - MinMidiNoteInViewport) / ViewportHeight);
    }

    public int PixelsToMilliseconds(float x)
    {
        Rect rect = RectTransformUtils.GetScreenCoordinates(rectTransform);
        return (int)(MinMillisecondsInViewport + ViewportWidth * ((x - rect.x) / rect.width));
    }

    public int PixelsToBeat(float x)
    {
        Rect rect = RectTransformUtils.GetScreenCoordinates(rectTransform);
        return (int)(MinBeatInViewport + ViewportWidthInBeats * ((x - rect.x) / rect.width));
    }

    public int PixelsToMidiNote(float y)
    {
        Rect rect = RectTransformUtils.GetScreenCoordinates(rectTransform);
        return (int)(MinMidiNoteInViewport + ViewportHeight * ((y - rect.y) / rect.height));
    }
}
