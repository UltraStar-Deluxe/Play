using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UniRx;
using System;
using System.ComponentModel;
using UnityEngine.UIElements;

#pragma warning disable CS0649

public class NoteAreaVerticalRulerControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private SongMeta songMeta;

    [Inject]
    private NoteArea noteArea;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject(UxmlName = R.UxmlNames.horizontalGridLabelContainer)]
    private VisualElement horizontalGridLabelContainer;

    [Inject(UxmlName = R.UxmlNames.horizontalGrid)]
    private VisualElement horizontalGrid;

    private DynamicTexture dynamicTexture;

    private ViewportEvent lastViewportEvent;

    public void OnInjectionFinished()
    {
        horizontalGrid.RegisterCallbackOneShot<GeometryChangedEvent>(evt =>
        {
            dynamicTexture = new DynamicTexture(songEditorSceneControl.gameObject, horizontalGrid);
            dynamicTexture.backgroundColor = new Color(0, 0, 0, 0);
            OnViewportChanged(lastViewportEvent);
        });

        noteArea.ViewportEventStream.Subscribe(OnViewportChanged);
    }

    private void OnViewportChanged(ViewportEvent viewportEvent)
    {
        if (viewportEvent == null)
        {
            return;
        }

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
        dynamicTexture.ClearTexture();

        int minMidiNote = noteArea.MinMidiNoteInViewport;
        int maxMidiNote = noteArea.MaxMidiNoteInViewport;
        for (int midiNote = minMidiNote; midiNote <= maxMidiNote; midiNote++)
        {
            // Notes are drawn on lines and between lines alternatingly.
            bool hasLine = (midiNote % 2 == 0);
            if (hasLine)
            {
                Color color = (MidiUtils.GetRelativePitch(midiNote) == 0)
                    ? NoteAreaHorizontalRulerControl.highlightLineColor
                    : NoteAreaHorizontalRulerControl.normalLineColor;
                DrawHorizontalGridLine(midiNote, color);
            }
        }

        dynamicTexture.ApplyTexture();
    }

    private void UpdateMidiNoteLabels()
    {
        horizontalGridLabelContainer.Clear();

        int minMidiNote = noteArea.MinMidiNoteInViewport;
        int maxMidiNote = noteArea.MaxMidiNoteInViewport;
        for (int midiNote = minMidiNote; midiNote <= maxMidiNote; midiNote++)
        {
            CreateLabelForMidiNote(midiNote);
        }
    }

    private void CreateLabelForMidiNote(int midiNote)
    {
        Label label = new Label();
        label.AddToClassList("tinyFont");
        label.style.position = new StyleEnum<Position>(Position.Absolute);
        label.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);

        float yPercent = (float)noteArea.GetVerticalPositionForMidiNote(midiNote);
        float heightPercent = noteArea.HeightForSingleNote;
        label.style.left = 0;
        label.style.bottom = new StyleLength(new Length(yPercent * 100, LengthUnit.Percent));
        label.style.height = new StyleLength(new Length(heightPercent * 100, LengthUnit.Percent));

        string midiNoteName = MidiUtils.GetAbsoluteName(midiNote);
        label.text = midiNoteName;

        horizontalGridLabelContainer.Add(label);
    }

    private void DrawHorizontalGridLine(int midiNote, Color color)
    {
        float yPercent = (float)noteArea.GetVerticalPositionForMidiNote(midiNote);
        int y = (int)(yPercent * dynamicTexture.TextureHeight);
        for (int x = 0; x < dynamicTexture.TextureWidth; x++)
        {
            dynamicTexture.SetPixel(x, y, color);
        }
    }
}
