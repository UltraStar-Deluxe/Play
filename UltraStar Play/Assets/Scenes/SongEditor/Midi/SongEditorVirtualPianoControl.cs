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

public class SongEditorVirtualPianoControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private NoteAreaControl noteAreaControl;

    [Inject]
    private Injector injector;

    [Inject(UxmlName = R.UxmlNames.virtualPiano)]
    private VisualElement virtualPiano;

    private ViewportEvent lastViewportEvent;

    public void OnInjectionFinished()
    {
        noteAreaControl.ViewportEventStream.Subscribe(OnViewportChanged);
    }

    private void OnViewportChanged(ViewportEvent viewportEvent)
    {
        if (lastViewportEvent == null
            || lastViewportEvent.Y != viewportEvent.Y
            || lastViewportEvent.Height != viewportEvent.Height)
        {
            UpdatePianoKeys();
        }
        lastViewportEvent = viewportEvent;
    }

    private void UpdatePianoKeys()
    {
        virtualPiano.Clear();

        int minMidiNote = noteAreaControl.MinMidiNoteInViewport;
        int maxMidiNote = noteAreaControl.MaxMidiNoteInViewport;
        for (int midiNote = minMidiNote; midiNote <= maxMidiNote; midiNote++)
        {
            CreatePianoKeyForMidiNote(midiNote);
        }
    }

    private void CreatePianoKeyForMidiNote(int midiNote)
    {
        VisualElement visualElement = new VisualElement();
        visualElement.AddToClassList("virtualPianoKey");
        VirtualPianoKeyControl keyControl = injector
            .WithRootVisualElement(visualElement)
            .CreateAndInject<VirtualPianoKeyControl>();
        keyControl.MidiNote = midiNote;

        float heightPercent = noteAreaControl.HeightForSingleNote * 0.8f;
        float yPercent = (float)noteAreaControl.GetVerticalPositionForMidiNote(midiNote) - heightPercent / 2;
        float widthPercent = MidiUtils.IsWhitePianoKey(midiNote) ? 0.9f : 0.7f;
        visualElement.style.position = new StyleEnum<Position>(Position.Absolute);
        visualElement.style.bottom = new StyleLength(new Length(100 * yPercent, LengthUnit.Percent));
        visualElement.style.height = new StyleLength(new Length(100 * heightPercent, LengthUnit.Percent));
        visualElement.style.left = 0;
        visualElement.style.width = new StyleLength(new Length(100 * widthPercent, LengthUnit.Percent));

        virtualPiano.Add(visualElement);
    }
}
