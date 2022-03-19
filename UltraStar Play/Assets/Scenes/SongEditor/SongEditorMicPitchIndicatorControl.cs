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

public class SongEditorMicPitchIndicatorControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = R.UxmlNames.micPitchOutOfRangeIndicatorTop)]
    private VisualElement micPitchOutOfRangeIndicatorTop;

    [Inject(UxmlName = R.UxmlNames.micPitchOutOfRangeIndicatorBottom)]
    private VisualElement micPitchOutOfRangeIndicatorBottom;

    [Inject]
    private MicPitchTracker micPitchTracker;

    [Inject]
    private Settings settings;

    [Inject]
    private NoteAreaControl noteAreaControl;

    public void OnInjectionFinished()
    {
        micPitchOutOfRangeIndicatorTop.HideByDisplay();
        micPitchOutOfRangeIndicatorBottom.HideByDisplay();

        micPitchTracker.PitchEventStream.Subscribe(pitchEvent =>
        {
            if (pitchEvent == null)
            {
                micPitchOutOfRangeIndicatorTop.HideByDisplay();
                micPitchOutOfRangeIndicatorBottom.HideByDisplay();
                return;
            }

            int shiftedMidiNote = pitchEvent.MidiNote + (settings.SongEditorSettings.MicOctaveOffset * 12);
            micPitchOutOfRangeIndicatorTop.SetVisibleByDisplay(shiftedMidiNote > noteAreaControl.MaxMidiNoteInCurrentViewport);
            micPitchOutOfRangeIndicatorBottom.SetVisibleByDisplay(shiftedMidiNote < noteAreaControl.MinMidiNoteInCurrentViewport);
        });
    }
}
