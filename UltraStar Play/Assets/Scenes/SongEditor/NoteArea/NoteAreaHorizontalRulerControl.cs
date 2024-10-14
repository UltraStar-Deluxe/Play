using System;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

#pragma warning disable CS0649

public class NoteAreaHorizontalRulerControl : INeedInjection, IInjectionFinishedListener
{
    public static readonly Color normalLineColor = Color.gray;
    public static readonly Color highlightLineColor = Color.white;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private NoteAreaControl noteAreaControl;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject(UxmlName = R.UxmlNames.verticalGridLabelContainer)]
    private VisualElement verticalGridLabelContainer;

    [Inject(UxmlName = R.UxmlNames.verticalGridLineContainer)]
    private VisualElement verticalGridLineContainer;

    [Inject(UxmlName = R.UxmlNames.verticalGrid)]
    private VisualElement verticalGrid;

    [Inject]
    private Settings settings;

    [Inject]
    private GameObject gameObject;

    private ViewportEvent lastViewportEvent;

    private double lastSongMetaBpm;

    private readonly VisualElementPool<Label> labelPool = new();

    public void OnInjectionFinished()
    {
        verticalGrid.RegisterCallbackOneShot<GeometryChangedEvent>(evt =>
        {
            UpdateLines();
            UpdateLabels();
        });

        noteAreaControl.ViewportEventStream.Subscribe(OnViewportChanged);

        settings.ObserveEveryValueChanged(_ => settings.SongEditorSettings.GridSizeInPx)
            .Subscribe(_ => UpdateLines())
            .AddTo(gameObject);

        settings.ObserveEveryValueChanged(_ => settings.SongEditorSettings.TimeLabelFormat)
            .Subscribe(_ => UpdateLabelTexts())
            .AddTo(gameObject);

        songMetaChangeEventStream.Subscribe(evt =>
        {
            if (evt
                is SongPropertyChangedEvent { SongProperty: ESongProperty.Gap }
                or LoadedMementoEvent)
            {
                UpdateLines();
                UpdateLabels();
            }
        });
    }

    private void UpdateLabelTexts()
    {
        if (labelPool.Count <= 0)
        {
            return;
        }

        labelPool.ForEach(label =>
        {
            if (label == null)
            {
                return;
            }
            int beat = (int)label.userData;
            double beatPosInMillis = SongMetaBpmUtils.BeatsToMillis(songMeta, beat);
            label.SetTranslatedText(GetLabelText(beat, beatPosInMillis));
        });
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
            || songMeta.BeatsPerMinute != lastSongMetaBpm)
        {
            lastSongMetaBpm = songMeta.BeatsPerMinute;

            if (settings.SongEditorSettings.GridSizeInPx > 0)
            {
                UpdateLines();
            }

            UpdateLabels();
        }
        lastViewportEvent = viewportEvent;
    }

    private void UpdateLines()
    {
        verticalGridLineContainer.Clear();

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
            double beatPosInMillis = SongMetaBpmUtils.BeatsToMillis(songMeta, beat);

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
    }

    private void UpdateLabels()
    {
        labelPool.FreeAllObjects();

        int viewportStartBeat = noteAreaControl.MinBeatInViewport;
        int viewportEndBeat = noteAreaControl.MaxBeatInViewport;
        int viewportWidthInBeats = viewportEndBeat - viewportStartBeat;

        int drawStepRough = viewportWidthInBeats / 12;
        if (viewportWidthInBeats <= 256)
        {
            drawStepRough = Math.Max(1, (int)Math.Log10(viewportWidthInBeats) * 8);
        }

        if (viewportWidthInBeats <= 48)
        {
            drawStepRough = 4;
        }

        double millisPerBeat = SongMetaBpmUtils.MillisPerBeat(songMeta);
        double labelWidthInMillis = millisPerBeat * drawStepRough;

        for (int beat = viewportStartBeat; beat < viewportEndBeat; beat++)
        {
            double beatPosInMillis = SongMetaBpmUtils.BeatsToMillis(songMeta, beat);

            bool hasRoughLine = drawStepRough > 0 && (beat % drawStepRough == 0);
            if (hasRoughLine)
            {
                if (!labelPool.TryGetFreeObject(out Label label))
                {
                    label = CreateLabel();
                    labelPool.AddUsedObject(label);
                }

                UpdateLabelPosition(label, beatPosInMillis, labelWidthInMillis);
                label.style.top = 0;
                label.SetTranslatedText(GetLabelText(beat, beatPosInMillis));
                label.userData = beat;
            }
        }
    }

    private Translation GetLabelText(int beat, double beatPosInMillis)
    {
        switch (settings.SongEditorSettings.TimeLabelFormat)
        {
            case ESongEditorTimeLabelFormat.Beats:
                return Translation.Of(beat.ToString());
            case ESongEditorTimeLabelFormat.Seconds:
                TimeSpan timeSpan = new(0, 0, 0, 0, (int)beatPosInMillis);
                double millisFraction = timeSpan.Milliseconds / 1000.0;
                return timeSpan.TotalMinutes > 0
                    ? Translation.Of($"{timeSpan.Seconds}{millisFraction.ToStringInvariantCulture(".0")}")
                    : Translation.Of($"{(int)timeSpan.TotalMinutes}:{timeSpan.Seconds:00}{millisFraction.ToStringInvariantCulture(".0")}");
            default:
                return Translation.Empty;
        }
    }

    private Label CreateLabel()
    {
        Label label = new();
        label.AddToClassList("noteAreaGridLabel");
        label.AddToClassList("verticalGridLabel");
        label.AddToClassList("tinyFont");
        label.style.position = new StyleEnum<Position>(Position.Absolute);
        label.style.unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleCenter);

        verticalGridLabelContainer.Add(label);
        return label;
    }

    private void UpdateLabelPosition(Label label, double beatPosInMillis, double labelWidthInMillis)
    {
        float widthPercent = (float)(labelWidthInMillis / noteAreaControl.ViewportWidth);
        float xPercent = (float)((beatPosInMillis - noteAreaControl.ViewportX) / noteAreaControl.ViewportWidth) - widthPercent / 2;
        label.style.left = new StyleLength(new Length(xPercent * 100, LengthUnit.Percent));
        label.style.width = new StyleLength(new Length(widthPercent * 100, LengthUnit.Percent));
    }

    private void DrawVerticalGridLine(double beatPosInMillis, Color color)
    {
        float lineWidth = settings.SongEditorSettings.GridSizeInPx;
        if (lineWidth <= 0)
        {
            return;
        }

        float xPercent = (float)((beatPosInMillis - noteAreaControl.ViewportX) / noteAreaControl.ViewportWidth);
        VisualElement line = new();
        line.AddToClassList("gridLine");
        line.AddToClassList("verticalGridLine");
        line.style.backgroundColor = color;
        line.style.left = new StyleLength(new Length(xPercent * 100, LengthUnit.Percent));
        line.style.width = new StyleLength(new Length(lineWidth, LengthUnit.Pixel));
        verticalGridLineContainer.Add(line);
    }
}
