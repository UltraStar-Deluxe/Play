using System;
using System.Globalization;
using UniInject;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS0649

public class NoteAreaRulerHorizontal : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public RectTransform fineLinePrefab;
    [InjectedInInspector]
    public RectTransform roughLinePrefab;

    [InjectedInInspector]
    public RectTransform lineContainer;

    [InjectedInInspector]
    public Text beatLabelPrefab;
    [InjectedInInspector]
    public Text secondLabelPrefab;

    [InjectedInInspector]
    public RectTransform beatLabelContainer;
    [InjectedInInspector]
    public RectTransform secondLabelContainer;

    [Inject(searchMethod = SearchMethods.GetComponentInParent)]
    private NoteArea noteArea;

    [Inject]
    private SongMeta songMeta;

    void Start()
    {
        UpdateLinesAndLabels();
    }

    public void UpdateLinesAndLabels()
    {
        lineContainer.DestroyAllDirectChildren();
        beatLabelContainer.DestroyAllDirectChildren();
        secondLabelContainer.DestroyAllDirectChildren();

        double viewportStartMillis = noteArea.GetMinMillisecondsInViewport();
        double viewportEndMillis = noteArea.GetMaxMillisecondsInViewport();
        int viewportStartBeat = (int)Math.Floor(BpmUtils.MillisecondInSongToBeat(songMeta, viewportStartMillis));
        int viewportEndBeat = (int)Math.Ceiling(BpmUtils.MillisecondInSongToBeat(songMeta, viewportEndMillis));
        int viewportWidthInBeats = viewportEndBeat - viewportStartBeat;

        double viewportWidthLog10 = Math.Log10(viewportWidthInBeats);

        int drawStepRough = Math.Max(1, (int)viewportWidthLog10 * 8);
        int drawStepVeryRough = drawStepRough * 2;
        int drawStepFine = 0;

        if (viewportWidthInBeats <= 128)
        {
            drawStepFine = drawStepRough / 2;
        }

        if (viewportWidthInBeats <= 48)
        {
            drawStepVeryRough = 6;
            drawStepRough = 4;
            drawStepFine = 1;
        }

        double millisPerBeat = BpmUtils.MillisecondsPerBeat(songMeta);
        double labelWidthInMillis = millisPerBeat * drawStepRough;

        for (int beat = viewportStartBeat; beat < viewportEndBeat; beat++)
        {
            double beatPosInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, beat);

            bool hasRoughLine = drawStepRough > 0 && (beat % drawStepRough == 0);
            if (hasRoughLine)
            {
                CreateLine(beatPosInMillis, viewportStartMillis, viewportEndMillis, roughLinePrefab);
                Text uiText = CreateLabel(beatPosInMillis, viewportStartMillis, viewportEndMillis, labelWidthInMillis, beatLabelPrefab, beatLabelContainer);
                uiText.text = beat.ToString();
            }

            bool hasFineLine = drawStepFine > 0 && (beat % drawStepFine == 0);
            if (hasFineLine && !hasRoughLine)
            {
                CreateLine(beatPosInMillis, viewportStartMillis, viewportEndMillis, fineLinePrefab);
            }

            bool hasSecondLabel = drawStepVeryRough > 0 && (beat % drawStepVeryRough == 0);
            if (hasSecondLabel)
            {
                double beatPosInSeconds = beatPosInMillis / 1000;
                Text uiText = CreateLabel(beatPosInMillis, viewportStartMillis, viewportEndMillis, labelWidthInMillis, secondLabelPrefab, secondLabelContainer);
                uiText.text = beatPosInSeconds.ToString("F3", CultureInfo.InvariantCulture) + " s";
            }
        }
    }

    private void CreateLine(double beatPosInMillis, double viewportStartMillis, double viewportEndMillis, RectTransform linePrefab)
    {
        RectTransform line = Instantiate(linePrefab, lineContainer);

        double viewportWidth = viewportEndMillis - viewportStartMillis;
        float x = (float)((beatPosInMillis - viewportStartMillis) / viewportWidth);
        line.anchorMin = new Vector2(x, 0);
        line.anchorMax = new Vector2(x, 1);
        line.anchoredPosition = Vector2.zero;
        line.sizeDelta = new Vector2(line.sizeDelta.x, 0);
    }

    private Text CreateLabel(double beatPosInMillis, double viewportStartMillis, double viewportEndMillis, double labelWidthInMillis, Text uiTextPrefab, RectTransform container)
    {
        Text uiText = Instantiate(uiTextPrefab, container);
        RectTransform label = uiText.GetComponent<RectTransform>();

        double viewportWidthInMillis = viewportEndMillis - viewportStartMillis;
        float x = (float)((beatPosInMillis - viewportStartMillis) / viewportWidthInMillis);
        float anchorWidth = (float)(labelWidthInMillis / viewportWidthInMillis);
        label.anchorMin = new Vector2(x - (anchorWidth / 2f), 0);
        label.anchorMax = new Vector2(x + (anchorWidth / 2f), 1);
        label.anchoredPosition = Vector2.zero;
        label.sizeDelta = new Vector2(0, 0);

        return uiText;
    }
}
