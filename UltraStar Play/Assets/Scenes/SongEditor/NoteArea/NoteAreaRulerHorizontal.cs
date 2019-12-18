using System;
using UniInject;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS0649

public class NoteAreaRulerHorizontal : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public RectTransform beatLinePrefab;
    [InjectedInInspector]
    public RectTransform beatLineContainer;

    public Text beatLabelPrefab;
    public RectTransform beatLabelContainer;

    [Inject(searchMethod = SearchMethods.GetComponentInParent)]
    private NoteArea noteArea;

    [Inject]
    private SongMeta songMeta;

    void Start()
    {
        UpdateBeatLinesAndLabels();
    }

    public void UpdateBeatLinesAndLabels()
    {
        beatLineContainer.DestroyAllDirectChildren<RectTransform>();
        beatLabelContainer.DestroyAllDirectChildren<RectTransform>();

        double viewportStartMillis = noteArea.GetMinMillisecondsInViewport();
        double viewportEndMillis = noteArea.GetMaxMillisecondsInViewport();
        int viewportStartBeat = (int)Math.Floor(BpmUtils.MillisecondInSongToBeat(songMeta, viewportStartMillis));
        int viewportEndBeat = (int)Math.Ceiling(BpmUtils.MillisecondInSongToBeat(songMeta, viewportEndMillis));

        int drawEveryN = (int)Math.Log10(viewportEndBeat - viewportStartBeat) * 10;
        if (drawEveryN < 1)
        {
            drawEveryN = 1;
        }
        double millisPerBeat = BpmUtils.MillisecondsPerBeat(songMeta);
        double labelWidthInMillis = millisPerBeat * drawEveryN;

        for (int beat = viewportStartBeat; beat < viewportEndBeat; beat++)
        {
            bool hasLine = (beat % drawEveryN == 0);
            if (hasLine)
            {
                double beatPosInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, beat);
                CreateLineForBeat(beatPosInMillis, viewportStartMillis, viewportEndMillis);
                CreateLabelForBeat(beat, beatPosInMillis, viewportStartMillis, viewportEndMillis, labelWidthInMillis);
            }
        }
    }

    private void CreateLineForBeat(double beatPosInMillis, double viewportStartMillis, double viewportEndMillis)
    {
        RectTransform line = Instantiate(beatLinePrefab, beatLineContainer);

        double viewportWidth = viewportEndMillis - viewportStartMillis;
        float x = (float)((beatPosInMillis - viewportStartMillis) / viewportWidth);
        line.anchorMin = new Vector2(x, 0);
        line.anchorMax = new Vector2(x, 1);
        line.anchoredPosition = Vector2.zero;
        line.sizeDelta = new Vector2(line.sizeDelta.x, 0);
    }

    private void CreateLabelForBeat(int beat, double beatPosInMillis, double viewportStartMillis, double viewportEndMillis, double labelWidthInMillis)
    {
        Text uiText = Instantiate(beatLabelPrefab, beatLabelContainer);
        RectTransform label = uiText.GetComponent<RectTransform>();

        double viewportWidthInMillis = viewportEndMillis - viewportStartMillis;
        float x = (float)((beatPosInMillis - viewportStartMillis) / viewportWidthInMillis);
        float anchorWidth = (float)(labelWidthInMillis / viewportWidthInMillis);
        label.anchorMin = new Vector2(x - (anchorWidth / 2f), 0);
        label.anchorMax = new Vector2(x + (anchorWidth / 2f), 1);
        label.anchoredPosition = Vector2.zero;
        label.sizeDelta = new Vector2(0, 0);

        uiText.text = beat.ToString();
    }
}
