using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UniRx;
using UnityEngine.UIElements;

#pragma warning disable CS0649

public class NoteAreaVerticalRulerControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private SongMeta songMeta;

    [Inject]
    private NoteAreaControl noteAreaControl;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private Settings settings;

    [Inject(UxmlName = R.UxmlNames.horizontalGridLabelContainer)]
    private VisualElement horizontalGridLabelContainer;

    [Inject(UxmlName = R.UxmlNames.horizontalGrid)]
    private VisualElement horizontalGrid;

    [Inject]
    private GameObject gameObject;

    private DynamicTexture dynamicTexture;

    private ViewportEvent lastViewportEvent;

    private readonly Label[] labels = new Label[NoteAreaControl.MaxMidiNoteInViewport + 1];

    public void OnInjectionFinished()
    {
        horizontalGrid.RegisterCallbackOneShot<GeometryChangedEvent>(evt =>
        {
            dynamicTexture = new DynamicTexture(songEditorSceneControl.gameObject, horizontalGrid);
            dynamicTexture.backgroundColor = new Color(0, 0, 0, 0);
            UpdateMidiNoteLabels();
            UpdateMidiNoteLines();
        });

        noteAreaControl.ViewportEventStream.Subscribe(OnViewportChanged);

        settings.ObserveEveryValueChanged(_ => settings.SongEditorSettings.GridSizeInDevicePixels)
            .Where(_ => dynamicTexture != null)
            .Subscribe(_ => UpdateMidiNoteLines())
            .AddTo(gameObject);
    }

    private void OnViewportChanged(ViewportEvent viewportEvent)
    {
        if (viewportEvent == null
            || dynamicTexture == null)
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

        int minMidiNote = noteAreaControl.MinMidiNoteInCurrentViewport;
        int maxMidiNote = noteAreaControl.MaxMidiNoteInCurrentViewport;
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
        int minMidiNote = noteAreaControl.MinMidiNoteInCurrentViewport;
        int maxMidiNote = noteAreaControl.MaxMidiNoteInCurrentViewport;

        for (int midiNote = NoteAreaControl.MinViewportY; midiNote < minMidiNote; midiNote++)
        {
            if (labels[midiNote] != null)
            {
                labels[midiNote].HideByDisplay();
            }
        }

        for (int midiNote = minMidiNote; midiNote <= maxMidiNote; midiNote++)
        {
            if (labels[midiNote] == null)
            {
                labels[midiNote] = CreateLabelForMidiNote(midiNote);
            }
            else
            {
                UpdateMidiNoteLabelPosition(labels[midiNote], midiNote);
                labels[midiNote].ShowByDisplay();
            }
        }

        for (int midiNote = maxMidiNote + 1; midiNote <= NoteAreaControl.MaxMidiNoteInViewport; midiNote++)
        {
            if (labels[midiNote] != null)
            {
                labels[midiNote].HideByDisplay();
            }
        }
    }

    private Label CreateLabelForMidiNote(int midiNote)
    {
        Label label = new Label();
        label.AddToClassList("tinyFont");
        label.enableRichText = false;
        label.style.position = new StyleEnum<Position>(Position.Absolute);
        label.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);
        label.style.left = 0;

        string midiNoteName = MidiUtils.GetAbsoluteName(midiNote);
        label.text = midiNoteName;

        UpdateMidiNoteLabelPosition(label, midiNote);

        horizontalGridLabelContainer.Add(label);

        return label;
    }

    private void UpdateMidiNoteLabelPosition(Label label, int midiNote)
    {
        float heightPercent = noteAreaControl.HeightForSingleNote;
        float yPercent = (float)noteAreaControl.GetVerticalPositionForMidiNote(midiNote) - heightPercent / 2;
        label.style.top = new StyleLength(new Length(yPercent * 100, LengthUnit.Percent));
        label.style.height = new StyleLength(new Length(heightPercent * 100, LengthUnit.Percent));
    }

    private void DrawHorizontalGridLine(int midiNote, Color color)
    {
        int height = settings.SongEditorSettings.GridSizeInDevicePixels;
        if (height <= 0)
        {
            return;
        }

        float yPercent = (float)noteAreaControl.GetVerticalPositionForMidiNote(midiNote);
        int fromY = (int)(yPercent * dynamicTexture.TextureHeight);
        int toY = fromY + height;
        for (int x = 0; x < dynamicTexture.TextureWidth; x++)
        {
            for (int y = fromY; y < toY && y < dynamicTexture.TextureHeight; y++)
            {
                dynamicTexture.SetPixel(x, y, color);
            }
        }
    }
}
