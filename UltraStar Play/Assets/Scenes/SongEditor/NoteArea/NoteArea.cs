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
    // The first midi note index that is visible in the viewport (index 0 would be MidiNoteMin)
    public int ViewportY { get; private set; } = (MidiUtils.MidiNoteMax - MidiUtils.MidiNoteMin) / 4;
    // The number of midi notes that are visible in the viewport
    public int ViewportHeight { get; private set; } = (MidiUtils.MidiNoteMax - MidiUtils.MidiNoteMin) / 2;

    // The viewport left side in the song in milliseconds
    public int ViewportX { get; private set; } = 0;
    // The width of the viewport in milliseconds
    public int ViewportWidth { get; private set; } = 3000;

    public bool IsPointerOver { get; private set; }

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private NoteAreaRulerVertical noteAreaRulerVertical;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private NoteAreaRulerHorizontal noteAreaRulerHorizontal;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongEditorSceneController songVideoPlayer;

    [Inject(searchMethod = SearchMethods.GetComponent)]
    private RectTransform rectTransform;

    private Subject<ViewportEvent> viewportEventStream = new Subject<ViewportEvent>();
    public ISubject<ViewportEvent> ViewportEventStream
    {
        get
        {
            return viewportEventStream;
        }
    }

    [Inject(searchMethod = SearchMethods.GetComponent)]
    private GraphicRaycaster graphicRaycaster;

    void Start()
    {
        ViewportEventStream.Subscribe(_ => UpdateNoteArea());
        songAudioPlayer.PositionInSongEventStream.Subscribe(SetPositionInSongInMillis);
        FireViewportChangedEvent();
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
            double newViewportX = ViewportX - viewportAutomaticScrollingLeft - positionInSongInMillis;
            SetViewportX((int)newViewportX);
        }
        else if (positionInSongInMillis > viewportAutomaticScrollingRight)
        {
            // Scroll right to new position
            double newViewportX = ViewportX + positionInSongInMillis - viewportAutomaticScrollingRight;
            SetViewportX((int)newViewportX);
        }
    }

    public void UpdateNoteArea()
    {
        noteAreaRulerHorizontal.UpdateLinesAndLabels();
        noteAreaRulerVertical.UpdatePitchLinesAndLabels();
    }

    public bool IsNoteVisible(Note note)
    {
        // Check y axis, which is the midi note
        bool isMidiNoteOk = (note.MidiNote >= GetMinMidiNoteInViewport())
                         && (note.MidiNote <= GetMaxMidiNoteInViewport());
        if (!isMidiNoteOk)
        {
            return false;
        }

        // Check x axis, which is the position in the song
        double startPosInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, note.StartBeat);
        double endPosInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, note.EndBeat);
        bool isMillisecondsOk = (startPosInMillis >= GetMinMillisecondsInViewport())
                             && (endPosInMillis <= GetMaxMillisecondsInViewport());
        return isMillisecondsOk;
    }

    public int GetMinMidiNoteInViewport()
    {
        return ViewportY + MidiUtils.MidiNoteMin;
    }

    public int GetMaxMidiNoteInViewport()
    {
        return GetMinMidiNoteInViewport() + ViewportHeight;
    }

    public double GetMinMillisecondsInViewport()
    {
        return ViewportX;
    }

    public double GetMaxMillisecondsInViewport()
    {
        return ViewportX + ViewportWidth;
    }

    public double GetMinBeatInViewport()
    {
        double minMillisInViewport = GetMinMillisecondsInViewport();
        double minBeatInViewport = BpmUtils.MillisecondInSongToBeat(songMeta, minMillisInViewport);
        return minBeatInViewport;
    }

    public double GetMaxBeatInViewport()
    {
        double maxMillisInViewport = GetMaxMillisecondsInViewport();
        double maxBeatInViewport = BpmUtils.MillisecondInSongToBeat(songMeta, maxMillisInViewport);
        return maxBeatInViewport;
    }

    public int GetVisibleMidiNoteCount()
    {
        return ViewportHeight;
    }

    public float GetVerticalPositionForIndexInViewport(int midiNoteIndexInViewport)
    {
        return (float)midiNoteIndexInViewport / ViewportHeight;
    }

    public float GetVerticalPositionForGeneralMidiNote(int midiNote)
    {
        int indexInViewport = midiNote - (ViewportY + MidiUtils.MidiNoteMin);
        return GetVerticalPositionForIndexInViewport(indexInViewport);
    }

    public float GetHeightForSingleNote()
    {
        return 1f / GetVisibleMidiNoteCount();
    }

    public float GetHorizontalPositionForMillis(int positionInSongInMillis)
    {
        return (float)(positionInSongInMillis - ViewportX) / ViewportWidth;
    }

    public float GetHorizontalPositionForBeat(int beat)
    {
        double positionInSongInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, beat);
        return GetHorizontalPositionForMillis((int)positionInSongInMillis);
    }

    public int GetMidiNote(int midiNoteIndexInViewport)
    {
        return MidiUtils.MidiNoteMin + ViewportY + midiNoteIndexInViewport;
    }

    public void ScrollHorizontal(int direction)
    {
        int newViewportX = ViewportX + direction * (int)(ViewportWidth * 0.2);
        SetViewportX(newViewportX);
    }

    public void ZoomHorizontal(int direction)
    {
        double zoomFactor = (direction > 0) ? 0.75 : 1.25;
        int newViewportWidth = (int)(ViewportWidth * zoomFactor);
        SetViewportWidth(newViewportWidth);
    }

    public void ScrollVertical(int direction)
    {
        int newViewportY = ViewportY + direction;
        SetViewportY(newViewportY);
    }

    public void ZoomVertical(int direction)
    {
        double zoomFactor = (direction > 0) ? 0.75 : 1.25;
        int newViewportHeight = (int)(ViewportHeight * zoomFactor);
        SetViewportHeight(newViewportHeight);
    }

    public void SetViewportWidth(int newViewportWidth)
    {
        if (newViewportWidth < 1000)
        {
            newViewportWidth = 1000;
        }
        if (newViewportWidth >= songAudioPlayer.DurationOfSongInMillis)
        {
            newViewportWidth = (int)songAudioPlayer.DurationOfSongInMillis;
        }

        if (newViewportWidth != ViewportWidth)
        {
            ViewportWidth = newViewportWidth;
            FireViewportChangedEvent();
        }
    }

    public void SetViewportX(int newViewportX)
    {
        if (newViewportX < 0)
        {
            newViewportX = 0;
        }
        if (newViewportX >= songAudioPlayer.DurationOfSongInMillis)
        {
            newViewportX = (int)songAudioPlayer.DurationOfSongInMillis;
        }

        if (newViewportX != ViewportX)
        {
            ViewportX = newViewportX;
            FireViewportChangedEvent();
        }
    }

    public void SetViewportY(int newViewportY)
    {
        if (newViewportY < 0)
        {
            newViewportY = 0;
        }
        if (newViewportY > MidiUtils.SingableNoteRange - ViewportHeight)
        {
            newViewportY = MidiUtils.SingableNoteRange - ViewportHeight;
        }

        if (newViewportY != ViewportY)
        {
            ViewportY = newViewportY;
            FireViewportChangedEvent();
        }
    }

    public void SetViewportHeight(int newViewportHeight)
    {
        if (newViewportHeight < 12)
        {
            newViewportHeight = 12;
        }
        if (newViewportHeight > 30)
        {
            newViewportHeight = 30;
        }

        if (newViewportHeight != ViewportHeight)
        {
            ViewportHeight = newViewportHeight;
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
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,
                                                                     ped.position,
                                                                     ped.pressEventCamera,
                                                                     out Vector2 localPoint))
        {
            return;
        }

        // Check that only the NoteArea was clicked, and not a note inside of it.
        List<RaycastResult> results = new List<RaycastResult>();
        graphicRaycaster.Raycast(ped, results);
        if (results.Count != 1 || results[0].gameObject != gameObject)
        {
            return;
        }

        float rectWidth = rectTransform.rect.width;
        double xPercent = (localPoint.x + (rectWidth / 2)) / rectWidth;
        double positionInSongInMillis = ViewportX + (ViewportWidth * xPercent);
        songAudioPlayer.PositionInSongInMillis = positionInSongInMillis;
    }
}
