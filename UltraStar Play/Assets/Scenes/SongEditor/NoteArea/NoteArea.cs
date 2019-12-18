using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UniInject;
using UniRx;

#pragma warning disable CS0649

public class NoteArea : MonoBehaviour, INeedInjection, IPointerEnterHandler, IPointerExitHandler
{
    // The first midi note index that is visible in the viewport (index 0 would be MidiNoteMin)
    private int viewportY = (MidiUtils.MidiNoteMax - MidiUtils.MidiNoteMin) / 4;
    // The number of midi notes that are visible in the viewport
    private int viewportHeight = (MidiUtils.MidiNoteMax - MidiUtils.MidiNoteMin) / 2;

    // The viewport left side in the song in milliseconds
    private int viewportX = 0;
    // The width of the viewport in milliseconds
    private int viewportWidth = 3000;

    public bool IsPointerOver { get; private set; }

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private NoteAreaRulerVertical noteAreaRulerVertical;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private NoteAreaRulerHorizontal noteAreaRulerHorizontal;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    private Subject<ViewportEvent> viewportEventStream = new Subject<ViewportEvent>();
    public ISubject<ViewportEvent> ViewportEventStream
    {
        get
        {
            return viewportEventStream;
        }
    }

    void Start()
    {
        UpdateNoteArea();
        ViewportEventStream.Subscribe(_ => UpdateNoteArea());
        songAudioPlayer.PositionInSongEventStream.Subscribe(SetPositionInSongInMillis);
    }

    public void SetPositionInSongInMillis(double positionInSongInMillis)
    {
        float viewportAutomaticScrollingLeft = viewportX + viewportWidth * 0.1f;
        float viewportAutomaticScrollingRight = viewportX + viewportWidth * 0.9f;
        if (positionInSongInMillis < viewportX || positionInSongInMillis > (viewportX + viewportWidth))
        {
            // Center viewport to position in song
            double newViewportX = positionInSongInMillis - viewportWidth / 2;
            SetViewportX((int)newViewportX);
        }
        else if (positionInSongInMillis < viewportAutomaticScrollingLeft)
        {
            // Scroll left to new position
            double newViewportX = viewportX - viewportAutomaticScrollingLeft - positionInSongInMillis;
            SetViewportX((int)newViewportX);
        }
        else if (positionInSongInMillis > viewportAutomaticScrollingRight)
        {
            // Scroll right to new position
            double newViewportX = viewportX + positionInSongInMillis - viewportAutomaticScrollingRight;
            SetViewportX((int)newViewportX);
        }
    }

    public void UpdateNoteArea()
    {
        noteAreaRulerHorizontal.UpdateBeatLinesAndLabels();
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
        return viewportY + MidiUtils.MidiNoteMin;
    }

    public int GetMaxMidiNoteInViewport()
    {
        return GetMinMidiNoteInViewport() + viewportHeight;
    }

    public double GetMinMillisecondsInViewport()
    {
        return viewportX;
    }

    public double GetMaxMillisecondsInViewport()
    {
        return viewportX + viewportWidth;
    }

    public int GetVisibleMidiNoteCount()
    {
        return viewportHeight;
    }

    public float GetVerticalPositionForIndexInViewport(int midiNoteIndexInViewport)
    {
        return (float)midiNoteIndexInViewport / viewportHeight;
    }

    public float GetVerticalPositionForGeneralMidiNote(int midiNote)
    {
        int indexInViewport = midiNote - viewportY;
        return GetVerticalPositionForIndexInViewport(indexInViewport);
    }

    public float GetHorizontalPositionForMillis(int positionInSongInMillis)
    {
        return (float)(positionInSongInMillis - viewportX) / viewportWidth;
    }

    public float GetHorizontalPositionForBeat(int beat)
    {
        double positionInSongInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, beat);
        return GetHorizontalPositionForMillis((int)positionInSongInMillis);
    }

    public int GetMidiNote(int midiNoteIndexInViewport)
    {
        return MidiUtils.MidiNoteMin + viewportY + midiNoteIndexInViewport;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        IsPointerOver = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        IsPointerOver = true;
    }

    public void ScrollHorizontal(int direction)
    {
        int newViewportX = viewportX + (int)(viewportWidth * 0.25);
        SetViewportX(newViewportX);
    }

    public void ZoomHorizontal(int direction)
    {
        double zoomFactor = (direction > 0) ? 0.75 : 1.25;
        int newViewportWidth = (int)(viewportWidth * zoomFactor);
        SetViewportWidth(newViewportWidth);
    }

    public void ScrollVertical(int direction)
    {
        int newViewportY = viewportY + direction;
        SetViewportY(newViewportY);
    }

    public void ZoomVertical(int direction)
    {
        double zoomFactor = (direction > 0) ? 0.75 : 1.25;
        int newViewportHeight = (int)(viewportHeight * zoomFactor);
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

        if (newViewportWidth != viewportWidth)
        {
            viewportWidth = newViewportWidth;
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

        if (newViewportX != viewportX)
        {
            viewportX = newViewportX;
            FireViewportChangedEvent();
        }
    }

    public void SetViewportY(int newViewportY)
    {
        if (newViewportY < 0)
        {
            newViewportY = 0;
        }
        if (newViewportY > MidiUtils.SingableNoteRange - viewportHeight)
        {
            newViewportY = MidiUtils.SingableNoteRange - viewportHeight;
        }

        if (newViewportY != viewportY)
        {
            viewportY = newViewportY;
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

        if (newViewportHeight != viewportHeight)
        {
            viewportHeight = newViewportHeight;
            FireViewportChangedEvent();
        }
    }

    private void FireViewportChangedEvent()
    {
        ViewportEvent viewportEvent = new ViewportEvent(viewportX, viewportY, viewportWidth, viewportHeight);
        viewportEventStream.OnNext(viewportEvent);
    }
}
