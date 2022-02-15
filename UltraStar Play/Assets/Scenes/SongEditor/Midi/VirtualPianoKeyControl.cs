using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class VirtualPianoKeyControl : INeedInjection, IInjectionFinishedListener
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

            if (MidiUtils.IsBlackPianoKey(midiNote))
            {
                visualElement.RemoveFromClassList("whiteKey");
                visualElement.AddToClassList("blackKey");
            }
            else
            {
                visualElement.AddToClassList("whiteKey");
                visualElement.RemoveFromClassList("blackKey");
            }
        }
    }

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    private VisualElement visualElement;

    [Inject]
    private MidiManager midiManager;

    public void OnInjectionFinished()
    {
        visualElement.RegisterCallback<PointerDownEvent>(evt => PlayMidiNote());
        visualElement.RegisterCallback<PointerUpEvent>(evt => StopMidiNote());
        visualElement.RegisterCallback<PointerLeaveEvent>(evt => StopMidiNote());
    }

    private void PlayMidiNote()
    {
        StopMidiNote();
        if (midiNote > -1)
        {
            midiManager.PlayMidiNote(midiNote);
        }
    }

    private void StopMidiNote()
    {
        if (midiNote > -1)
        {
            midiManager.StopMidiNote(midiNote);
        }
    }
}
