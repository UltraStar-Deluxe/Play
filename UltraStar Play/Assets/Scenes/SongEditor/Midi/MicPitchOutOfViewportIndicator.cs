using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MicPitchOutOfViewportIndicator : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public Image pitchAboveImage;
    [InjectedInInspector]
    public Image pitchBelowImage;

    [Inject]
    private MicPitchTracker micPitchTracker;

    [Inject]
    private Settings settings;

    [Inject]
    private NoteAreaControl noteAreaControl;

    void Start()
    {
        pitchAboveImage.SetAlpha(0);
        pitchBelowImage.SetAlpha(0);

        micPitchTracker.PitchEventStream.Subscribe(pitchEvent =>
        {
            if (pitchEvent == null)
            {
                pitchAboveImage.SetAlpha(0);
                pitchBelowImage.SetAlpha(0);
                return;
            }
            int shiftedMidiNote = pitchEvent.MidiNote + (settings.SongEditorSettings.MicOctaveOffset * 12);
            pitchAboveImage.SetAlpha(shiftedMidiNote > noteAreaControl.MaxMidiNoteInViewport ? 1 : 0);
            pitchBelowImage.SetAlpha(shiftedMidiNote < noteAreaControl.MinMidiNoteInViewport ? 1 : 0);
        });
    }
}
