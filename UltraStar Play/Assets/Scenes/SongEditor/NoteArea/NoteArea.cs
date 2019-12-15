using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UniInject;

#pragma warning disable CS0649

public class NoteArea : MonoBehaviour, INeedInjection, IPointerEnterHandler, IPointerExitHandler
{
    // Zoom of 1 is defined as "all the song is visible", a zoom of 2 would be "half the song is visible".
    // The zoom factor is given in per mille (i.e. 1/1000).
    // public int zoomXPerMille = 1000;

    // Zoom of 1 is defined as "all the notes are visible", a zoom of 2 would be "half the notes are visible".
    // The zoom factor is given in per cent (i.e. 1/100).
    // public int zoomYPerCent = 100;

    // The first midi note index that is visible in the viewport (index 0 would be midiNoteMin)
    public int viewportY = (MidiUtils.midiNoteMax - MidiUtils.midiNoteMin) / 4;
    public float viewportX = 0;

    // The number of midi notes that are visible in the viewport
    public int viewportHeight = (MidiUtils.midiNoteMax - MidiUtils.midiNoteMin) / 2;

    public bool isPointerOver;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private NoteAreaLines noteAreaLines;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private NoteAreaLineLabels noteAreaLineLabels;

    void Update()
    {
        // Scroll with mouse wheel
        if (isPointerOver)
        {
            int scroll = Math.Sign(Input.mouseScrollDelta.y);
            if (scroll > 0 && (viewportY + MidiUtils.midiNoteMin) <= (MidiUtils.midiNoteMax - viewportHeight))
            {
                viewportY += scroll;
                UpdateNoteArea();
            }
            else if (scroll < 0 && viewportY > 0)
            {
                viewportY += scroll;
                UpdateNoteArea();
            }
        }
    }

    private void UpdateNoteArea()
    {
        noteAreaLines.UpdateLines();
        noteAreaLineLabels.UpdateLabels();
    }

    public int GetVisibleMidiNoteCount()
    {
        return viewportHeight;
    }

    public float GetVisibleMidiNotePositionY(int midiNoteIndexInViewport)
    {
        return (float)midiNoteIndexInViewport / viewportHeight;
    }

    public float GetMidiNotePositionY(int midiNote)
    {
        return (float)(midiNote - viewportY) / viewportHeight;
    }

    public int GetMidiNote(int midiNoteIndexInViewport)
    {
        return MidiUtils.midiNoteMin + viewportY + midiNoteIndexInViewport;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
    }
}
