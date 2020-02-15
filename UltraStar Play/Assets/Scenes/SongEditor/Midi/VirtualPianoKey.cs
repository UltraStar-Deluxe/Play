using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.EventSystems;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class VirtualPianoKey : MonoBehaviour, INeedInjection, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    private int midiNote = -1;
    public int MidiNote
    {
        get
        {
            return midiNote;
        }
        set
        {
            if (midiNote != -1)
            {
                midiManager.StopMidiNote(midiNote);
            }

            midiNote = value;

            Color keyColor = MidiUtils.IsBlackPianoKey(midiNote) ? Colors.black : Colors.white;
            keyImage.color = keyColor;
        }
    }

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private Image keyImage;

    [Inject]
    private MidiManager midiManager;

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Needed for OnPointer events
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Needed for OnPointer events
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (midiNote > -1)
        {
            midiManager.PlayMidiNote(midiNote);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (midiNote > -1)
        {
            midiManager.StopMidiNote(midiNote);
        }
    }
}
