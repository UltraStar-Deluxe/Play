using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;

#pragma warning disable CS0649

public class NoteAreaRulerVertical : MonoBehaviour, INeedInjection, ISceneInjectionFinishedListener
{
    [InjectedInInspector]
    public Text labelPrefab;
    [InjectedInInspector]
    public RectTransform labelContainer;

    [InjectedInInspector]
    public DynamicallyCreatedImage horizontalGridImage;

    [Inject(searchMethod = SearchMethods.GetComponentInParent)]
    private readonly NoteArea noteArea;

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
            UpdateMidiNoteLabels();
            UpdateMidiNoteLines();
        }
        lastViewportEvent = viewportEvent;
    }

    private void UpdateMidiNoteLines()
    {
        horizontalGridImage.ClearTexture();

        int minMidiNote = noteArea.MinMidiNoteInViewport;
        int maxMidiNote = noteArea.MaxMidiNoteInViewport;
        for (int midiNote = minMidiNote; midiNote <= maxMidiNote; midiNote++)
        {
            // Notes are drawn on lines and between lines alternatingly.
            bool hasLine = (midiNote % 2 == 0);
            if (hasLine)
            {
                DrawHorizontalGridLine(midiNote, Color.white);
            }
        }

        horizontalGridImage.ApplyTexture();
    }

    private void UpdateMidiNoteLabels()
    {
        labelContainer.DestroyAllDirectChildren();

        int minMidiNote = noteArea.MinMidiNoteInViewport;
        int maxMidiNote = noteArea.MaxMidiNoteInViewport;
        for (int midiNote = minMidiNote; midiNote <= maxMidiNote; midiNote++)
        {
            CreateLabelForMidiNote(midiNote);
        }
    }

    private void CreateLabelForMidiNote(int midiNote)
    {
        Text uiText = Instantiate(labelPrefab, labelContainer);
        RectTransform label = uiText.GetComponent<RectTransform>();

        float y = (float)noteArea.GetVerticalPositionForMidiNote(midiNote);
        float anchorHeight = noteArea.HeightForSingleNote;
        label.anchorMin = new Vector2(0, y - (anchorHeight / 2f));
        label.anchorMax = new Vector2(1, y + (anchorHeight / 2f));
        label.anchoredPosition = Vector2.zero;
        label.sizeDelta = new Vector2(0, 0);

        string midiNoteName = MidiUtils.GetAbsoluteName(midiNote);
        uiText.text = midiNoteName;
    }

    private void DrawHorizontalGridLine(int midiNote, Color color)
    {
        float yPercent = (float)noteArea.GetVerticalPositionForMidiNote(midiNote);
        int y = (int)(yPercent * horizontalGridImage.TextureHeight);
        for (int x = 0; x < horizontalGridImage.TextureWidth; x++)
        {
            horizontalGridImage.SetPixel(x, y, color);
        }
    }
}
