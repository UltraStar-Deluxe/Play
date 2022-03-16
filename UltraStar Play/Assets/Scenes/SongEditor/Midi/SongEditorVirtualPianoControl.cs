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

    [Inject]
    private Settings settings;

    private ViewportEvent lastViewportEvent;

    private readonly VirtualPianoKeyControl[] virtualPianoKeyControls = new VirtualPianoKeyControl[MidiUtils.MaxMidiNote + 1];

    public void OnInjectionFinished()
    {
        virtualPiano.Clear();

        // Creating lots of VisualElements every frame has bad performance.
        // Instead, a pool of VisualElements is created once here and then hidden/shown and positioned every frame as needed.
        for (int midiNote = 0; midiNote <= MidiUtils.MaxMidiNote; midiNote++)
        {
            virtualPianoKeyControls[midiNote] = CreatePianoKeyForMidiNote(midiNote);
            virtualPianoKeyControls[midiNote].Hide();
        }

        noteAreaControl.ViewportEventStream
            .Subscribe(OnViewportChanged);

        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.ShowVirtualPianoArea)
            .Subscribe(newValue => UpdatePianoKeys(newValue));
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

    private void UpdatePianoKeys(bool force = false)
    {
        if (!virtualPiano.IsVisibleByDisplay()
            && !force)
        {
            return;
        }

        int minMidiNote = noteAreaControl.MinMidiNoteInCurrentViewport;
        int maxMidiNote = noteAreaControl.MaxMidiNoteInCurrentViewport;
        for (int midiNote = 0; midiNote < minMidiNote && midiNote <= MidiUtils.MaxMidiNote; midiNote++)
        {
            virtualPianoKeyControls[midiNote].Hide();
        }

        for (int midiNote = minMidiNote; midiNote <= maxMidiNote && midiNote <= MidiUtils.MaxMidiNote; midiNote++)
        {
            virtualPianoKeyControls[midiNote].Show();
            UpdatePosition(virtualPianoKeyControls[midiNote]);
        }

        for (int midiNote = maxMidiNote + 1; midiNote <= MidiUtils.MaxMidiNote; midiNote++)
        {
            virtualPianoKeyControls[midiNote].Hide();
        }
    }

    private VirtualPianoKeyControl CreatePianoKeyForMidiNote(int midiNote)
    {
        VisualElement visualElement = new VisualElement();
        visualElement.AddToClassList("virtualPianoKey");
        VirtualPianoKeyControl keyControl = injector
            .WithRootVisualElement(visualElement)
            .CreateAndInject<VirtualPianoKeyControl>();
        keyControl.MidiNote = midiNote;

        virtualPiano.Add(visualElement);

        return keyControl;
    }

    private void UpdatePosition(VirtualPianoKeyControl keyControl)
    {
        float heightPercent = noteAreaControl.HeightForSingleNote * 0.8f;
        float yPercent = (float)noteAreaControl.GetVerticalPositionForMidiNote(keyControl.MidiNote) - heightPercent / 2;
        float widthPercent = MidiUtils.IsWhitePianoKey(keyControl.MidiNote) ? 0.9f : 0.7f;

        VisualElement visualElement = keyControl.VisualElement;
        visualElement.style.position = new StyleEnum<Position>(Position.Absolute);
        visualElement.style.top = new StyleLength(new Length(yPercent * 100, LengthUnit.Percent));
        visualElement.style.height = new StyleLength(new Length(heightPercent * 100, LengthUnit.Percent));
        visualElement.style.left = 0;
        visualElement.style.width = new StyleLength(new Length(widthPercent * 100, LengthUnit.Percent));
    }
}
