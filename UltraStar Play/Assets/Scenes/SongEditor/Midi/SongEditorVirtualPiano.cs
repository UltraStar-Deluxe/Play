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

public class SongEditorVirtualPiano : MonoBehaviour, INeedInjection, ISceneInjectionFinishedListener
{
    [InjectedInInspector]
    public VirtualPianoKey pianoKeyPrefab;
    [InjectedInInspector]
    public RectTransform pianoKeyContainer;

    [Inject]
    private NoteArea noteArea;

    [Inject]
    private Injector injector;

    private ViewportEvent lastViewportEvent;

    public void OnSceneInjectionFinished()
    {
        noteArea.ViewportEventStream.Subscribe(OnViewportChanged);
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
        pianoKeyContainer.DestroyAllDirectChildren();

        int minMidiNote = noteArea.MinMidiNoteInViewport;
        int maxMidiNote = noteArea.MaxMidiNoteInViewport;
        for (int midiNote = minMidiNote; midiNote <= maxMidiNote; midiNote++)
        {
            CreatePianoKeyForMidiNote(midiNote);
        }
    }

    private void CreatePianoKeyForMidiNote(int midiNote)
    {
        VirtualPianoKey key = Instantiate(pianoKeyPrefab, pianoKeyContainer);
        injector.Inject(key);
        key.MidiNote = midiNote;

        RectTransform keyRectTransform = key.GetComponent<RectTransform>();
        float y = (float)noteArea.GetVerticalPositionForMidiNote(midiNote);
        float anchorHeight = noteArea.HeightForSingleNote * 0.8f;
        float xMax = MidiUtils.IsWhitePianoKey(midiNote) ? 0.9f : 0.7f;
        keyRectTransform.anchorMin = new Vector2(0, y - (anchorHeight / 2f));
        keyRectTransform.anchorMax = new Vector2(xMax, y + (anchorHeight / 2f));
        keyRectTransform.anchoredPosition = Vector2.zero;
        keyRectTransform.sizeDelta = new Vector2(0, 0);
    }
}
