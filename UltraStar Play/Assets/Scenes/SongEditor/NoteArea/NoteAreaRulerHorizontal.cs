using System;
using System.Globalization;
using UniInject;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

#pragma warning disable CS0649

public class NoteAreaRulerHorizontal : MonoBehaviour, INeedInjection, ISceneInjectionFinishedListener
{
    [InjectedInInspector]
    public Text beatLabelPrefab;
    [InjectedInInspector]
    public Text secondLabelPrefab;

    [InjectedInInspector]
    public RectTransform beatLabelContainer;
    [InjectedInInspector]
    public RectTransform secondLabelContainer;

    [InjectedInInspector]
    public DynamicallyCreatedImage verticalGridImage;

    [Inject(searchMethod = SearchMethods.GetComponentInParent)]
    private NoteArea noteArea;

    [Inject]
    private SongMeta songMeta;

    private ViewportEvent lastViewportEvent;

    private float lastSongMetaBpm;

    public void OnSceneInjectionFinished()
    {
        noteArea.ViewportEventStream.Subscribe(OnViewportChanged);
    }

    private void OnViewportChanged(ViewportEvent viewportEvent)
    {
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
        verticalGridImage.ClearTexture();

        int viewportStartBeat = noteArea.MinBeatInViewport;
        int viewportEndBeat = noteArea.MaxBeatInViewport;
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
                DrawVerticalGridLine(beatPosInMillis, Color.white);
            }

            bool hasFineLine = drawStepFine > 0 && (beat % drawStepFine == 0);
            if (hasFineLine && !hasRoughLine)
            {
                DrawVerticalGridLine(beatPosInMillis, Color.gray);
            }
        }

        verticalGridImage.ApplyTexture();
    }

    private void UpdateLabels()
    {
        beatLabelContainer.DestroyAllDirectChildren();
        secondLabelContainer.DestroyAllDirectChildren();

        int viewportStartBeat = noteArea.MinBeatInViewport;
        int viewportEndBeat = noteArea.MaxBeatInViewport;
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
                Text uiText = CreateLabel(beatPosInMillis, labelWidthInMillis, beatLabelPrefab, beatLabelContainer);
                uiText.text = beat.ToString();
            }

            bool hasSecondLabel = drawStepVeryRough > 0 && (beat % drawStepVeryRough == 0);
            if (hasSecondLabel)
            {
                double beatPosInSeconds = beatPosInMillis / 1000;
                Text uiText = CreateLabel(beatPosInMillis, labelWidthInMillis, secondLabelPrefab, secondLabelContainer);
                uiText.text = beatPosInSeconds.ToString("F3", CultureInfo.InvariantCulture) + " s";
            }
        }
    }

    private Text CreateLabel(double beatPosInMillis, double labelWidthInMillis, Text uiTextPrefab, RectTransform container)
    {
        Text uiText = Instantiate(uiTextPrefab, container);
        RectTransform label = uiText.GetComponent<RectTransform>();

        float x = (float)((beatPosInMillis - noteArea.ViewportX) / noteArea.ViewportWidth);
        float anchorWidth = (float)(labelWidthInMillis / noteArea.ViewportWidth);
        label.anchorMin = new Vector2(x - (anchorWidth / 2f), 0);
        label.anchorMax = new Vector2(x + (anchorWidth / 2f), 1);
        label.anchoredPosition = Vector2.zero;
        label.sizeDelta = new Vector2(0, 0);

        return uiText;
    }

    private void DrawVerticalGridLine(double beatPosInMillis, Color color)
    {
        double xPercent = (beatPosInMillis - noteArea.ViewportX) / noteArea.ViewportWidth;
        int x = (int)(xPercent * verticalGridImage.TextureWidth);
        for (int y = 0; y < verticalGridImage.TextureHeight; y++)
        {
            verticalGridImage.SetPixel(x, y, color);
        }
    }
}
