﻿using System;
using System.Globalization;
using UniInject;
using UnityEngine;
using UniRx;
using UnityEngine.UIElements;

#pragma warning disable CS0649

public class NoteAreaHorizontalRulerControl : INeedInjection, IInjectionFinishedListener
{
    public static readonly Color normalLineColor = Color.gray;
    public static readonly Color highlightLineColor = Color.white;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private NoteAreaControl noteAreaControl;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject(UxmlName = R.UxmlNames.verticalGridLabelContainer)]
    private VisualElement verticalGridLabelContainer;

    [Inject(UxmlName = R.UxmlNames.verticalGrid)]
    private VisualElement verticalGrid;

    private DynamicTexture dynamicTexture;

    private ViewportEvent lastViewportEvent;

    private float lastSongMetaBpm;

    public void OnInjectionFinished()
    {
        verticalGrid.RegisterCallbackOneShot<GeometryChangedEvent>(evt =>
        {
            dynamicTexture = new DynamicTexture(songEditorSceneControl.gameObject, verticalGrid);
            dynamicTexture.backgroundColor = new Color(0, 0, 0, 0);
            UpdateLines();
            UpdateLabels();
        });

        noteAreaControl.ViewportEventStream.Subscribe(OnViewportChanged);
    }

    private void OnViewportChanged(ViewportEvent viewportEvent)
    {
        if (viewportEvent == null)
        {
            return;
        }

        if (lastViewportEvent == null
            || lastViewportEvent.X != viewportEvent.X
            || lastViewportEvent.Width != viewportEvent.Width
            || songMeta.Bpm != lastSongMetaBpm)
        {
            lastSongMetaBpm = songMeta.Bpm;
            UpdateLines();
            UpdateLabels();
        }
        lastViewportEvent = viewportEvent;
    }

    private void UpdateLines()
    {
        if (dynamicTexture == null)
        {
            return;
        }
        dynamicTexture.ClearTexture();

        int viewportStartBeat = noteAreaControl.MinBeatInViewport;
        int viewportEndBeat = noteAreaControl.MaxBeatInViewport;
        int viewportWidthInBeats = viewportEndBeat - viewportStartBeat;

        int drawStepRough = viewportWidthInBeats / 12;
        if (viewportWidthInBeats <= 256)
        {
            drawStepRough = Math.Max(1, (int)Math.Log10(viewportWidthInBeats) * 8);
        }

        int drawStepFine = 0;

        // Draw additional lines if zoomed in enough
        if (viewportWidthInBeats <= 128)
        {
            drawStepFine = drawStepRough / 2;
        }

        // Draw every line if zoomed in enough
        if (viewportWidthInBeats <= 48)
        {
            drawStepRough = 4;
            drawStepFine = 1;
        }

        for (int beat = viewportStartBeat; beat < viewportEndBeat; beat++)
        {
            double beatPosInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, beat);

            bool hasRoughLine = drawStepRough > 0 && (beat % drawStepRough == 0);
            if (hasRoughLine)
            {
                DrawVerticalGridLine(beatPosInMillis, highlightLineColor);
            }

            bool hasFineLine = drawStepFine > 0 && (beat % drawStepFine == 0);
            if (hasFineLine && !hasRoughLine)
            {
                DrawVerticalGridLine(beatPosInMillis, normalLineColor);
            }
        }

        dynamicTexture.ApplyTexture();
    }

    private void UpdateLabels()
    {
        verticalGridLabelContainer.Clear();

        int viewportStartBeat = noteAreaControl.MinBeatInViewport;
        int viewportEndBeat = noteAreaControl.MaxBeatInViewport;
        int viewportWidthInBeats = viewportEndBeat - viewportStartBeat;

        int drawStepRough = viewportWidthInBeats / 12;
        if (viewportWidthInBeats <= 256)
        {
            drawStepRough = Math.Max(1, (int)Math.Log10(viewportWidthInBeats) * 8);
        }

        int drawStepVeryRough = drawStepRough * 2;

        if (viewportWidthInBeats <= 48)
        {
            drawStepVeryRough = 6;
            drawStepRough = 4;
        }

        double millisPerBeat = BpmUtils.MillisecondsPerBeat(songMeta);
        double labelWidthInMillis = millisPerBeat * drawStepRough;

        for (int beat = viewportStartBeat; beat < viewportEndBeat; beat++)
        {
            double beatPosInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, beat);

            bool hasRoughLine = drawStepRough > 0 && (beat % drawStepRough == 0);
            if (hasRoughLine)
            {
                Label label = CreateLabel(beatPosInMillis, labelWidthInMillis, verticalGridLabelContainer);
                label.text = beat.ToString();
                label.style.top = 12;
            }

            bool hasSecondLabel = drawStepVeryRough > 0 && (beat % drawStepVeryRough == 0);
            if (hasSecondLabel)
            {
                double beatPosInSeconds = beatPosInMillis / 1000;
                Label label = CreateLabel(beatPosInMillis, labelWidthInMillis, verticalGridLabelContainer);
                label.text = beatPosInSeconds.ToString("F3", CultureInfo.InvariantCulture) + " s";
            }
        }
    }

    private Label CreateLabel(double beatPosInMillis, double labelWidthInMillis, VisualElement container)
    {
        Label label = new Label();
        label.AddToClassList("tinyFont");
        label.style.position = new StyleEnum<Position>(Position.Absolute);
        label.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);

        float widthPercent = (float)(labelWidthInMillis / noteAreaControl.ViewportWidth);
        float xPercent = (float)((beatPosInMillis - noteAreaControl.ViewportX) / noteAreaControl.ViewportWidth) - widthPercent / 2;
        label.style.left = new StyleLength(new Length(xPercent * 100, LengthUnit.Percent));
        label.style.top = 0;
        label.style.width = new StyleLength(new Length(widthPercent * 100, LengthUnit.Percent));
        container.Add(label);
        return label;
    }

    private void DrawVerticalGridLine(double beatPosInMillis, Color color)
    {
        double xPercent = (beatPosInMillis - noteAreaControl.ViewportX) / noteAreaControl.ViewportWidth;
        int x = (int)(xPercent * dynamicTexture.TextureWidth);
        for (int y = 0; y < dynamicTexture.TextureHeight; y++)
        {
            dynamicTexture.SetPixel(x, y, color);
        }
    }
}