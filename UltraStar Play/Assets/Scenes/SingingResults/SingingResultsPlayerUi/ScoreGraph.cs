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

public class ScoreGraph : MonoBehaviour, INeedInjection, IExcludeFromSceneInjection, IInjectionFinishedListener
{
    [InjectedInInspector]
    public RectTransform perfectBeatBar;

    [InjectedInInspector]
    public RectTransform goodBeatBar;

    [InjectedInInspector]
    public RectTransform missedBeatBar;

    [InjectedInInspector]
    public RectTransform dataPointsContainer;

    [InjectedInInspector]
    public ScoreGraphDataPoint scoreGraphDataPointPrefab;

    [InjectedInInspector]
    public Color lineColor = Color.gray;

    [InjectedInInspector]
    public float lineThickness = 3;

    [Inject]
    private Injector injector;

    [Inject]
    private PlayerScoreControllerData playerScoreData;

    [Inject]
    private SingingResultsSceneData sceneData;

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private DynamicallyCreatedImage dynamicallyCreatedImage;

    public void OnInjectionFinished()
    {
        InitBeatBars();
        InitDataPoints();
    }

    private void InitBeatBars()
    {
        // The done beats are approximated using the done sentences (a sentence that might have started but not finished will not be not inclueded).
        int totalBeatCount = playerScoreData.NormalNoteLengthTotal + playerScoreData.GoldenNoteLengthTotal;
        int doneBeatCount = playerScoreData.SentenceToSentenceScoreMap.Keys
            .SelectMany(sentence => sentence.Notes)
            .Where(note => !note.IsFreestyle)
            .Select(note => note.Length)
            .Sum();
        float doneBeatPercent = totalBeatCount > 0 ? (float)doneBeatCount / totalBeatCount : 0;
        float perfectBeatPercent = CalculatePerfectBeatPercent(totalBeatCount);
        float goodBeatPercent = CalculateGoodBeatPercent(totalBeatCount);
        float missedBeatPercent = doneBeatPercent - (perfectBeatPercent + goodBeatPercent);

        PositionBeatBar(missedBeatBar, 0, missedBeatPercent);
        PositionBeatBar(goodBeatBar, missedBeatPercent, missedBeatPercent + goodBeatPercent);
        PositionBeatBar(perfectBeatBar, missedBeatPercent + goodBeatPercent, doneBeatPercent);
    }

    private void InitDataPoints()
    {
        // Remove dummy data points
        foreach (ScoreGraphDataPoint dataPoint in dataPointsContainer.GetComponentsInChildren<ScoreGraphDataPoint>())
        {
            Destroy(dataPoint.gameObject);
        }

        if (sceneData.SongDurationInMillis <= 0)
        {
            return;
        }

        float songGap = sceneData.SongMeta.Gap;

        List<Vector2> anchorPositions = new List<Vector2>();
        anchorPositions.Add(Vector2.zero);
        foreach (SentenceScore sentenceScore in playerScoreData.SentenceToSentenceScoreMap.Values)
        {
            double scorePercent = (double)sentenceScore.TotalScoreSoFar / PlayerScoreController.MaxScore;

            double sentenceEndInMillis = BpmUtils.BeatToMillisecondsInSong(sceneData.SongMeta, sentenceScore.Sentence.MaxBeat);
            double timePercent = (sentenceEndInMillis - songGap) / (sceneData.SongDurationInMillis - songGap);

            Vector2 anchorPosition = new Vector2((float)timePercent, (float)scorePercent);
            CreateDataPoint(anchorPosition, (int)sentenceEndInMillis, sentenceScore.TotalScoreSoFar);

            anchorPositions.Add(anchorPosition);
        }
        anchorPositions.Add(new Vector2(1, anchorPositions.Last().y));

        DrawGraphLine(anchorPositions);
    }

    private void DrawGraphLine(List<Vector2> anchorPositions)
    {
        dynamicallyCreatedImage.GetComponent<RawImage>().enabled = true;
        dynamicallyCreatedImage.ClearTexture();

        DrawXYAxis();

        int w = dynamicallyCreatedImage.TextureWidth;
        int h = dynamicallyCreatedImage.TextureHeight;

        bool isFirst = true;
        Vector2 lastPoint = Vector2.zero;
        foreach (Vector2 point in anchorPositions)
        {
            if (isFirst)
            {
                isFirst = false;
            }
            else
            {
                dynamicallyCreatedImage.DrawLine((int)(lastPoint.x * w),
                    (int)(lastPoint.y * h),
                    (int)(point.x * w),
                    (int)(point.y * h),
                    lineThickness,
                    lineColor);
            }
            lastPoint = point;
        }

        dynamicallyCreatedImage.ApplyTexture();
    }

    private void DrawXYAxis()
    {
        dynamicallyCreatedImage.DrawLine(0, 0, 0, dynamicallyCreatedImage.TextureHeight, 2, lineColor);
        dynamicallyCreatedImage.DrawLine(0, 0, dynamicallyCreatedImage.TextureWidth, 0, 2, lineColor);
    }

    private ScoreGraphDataPoint CreateDataPoint(Vector2 anchorPosition, int sentenceEndInMillis, int totalScoreSoFar)
    {
        ScoreGraphDataPoint dataPoint = Instantiate(scoreGraphDataPointPrefab, dataPointsContainer);
        injector.InjectAllComponentsInChildren(dataPoint);
        dataPoint.CoordinateDetails = FormatMilliseconds(sentenceEndInMillis) + "\n" + totalScoreSoFar + " points";
        dataPoint.RectTransform.anchorMin = anchorPosition;
        dataPoint.RectTransform.anchorMax = anchorPosition;
        dataPoint.RectTransform.MoveCornersToAnchors_CenterPosition();
        return dataPoint;
    }

    private void PositionBeatBar(RectTransform beatBarRectTransform, float anchorMinY, float anchorMaxY)
    {
        beatBarRectTransform.anchorMin = new Vector2(beatBarRectTransform.anchorMin.x, anchorMinY);
        beatBarRectTransform.anchorMax = new Vector2(beatBarRectTransform.anchorMax.x, anchorMaxY);
        beatBarRectTransform.MoveCornersToAnchors();
    }

    private float CalculatePerfectBeatPercent(int totalBeatCount)
    {
        if (totalBeatCount == 0)
        {
            return 0;
        }

        int perfectBeatsTotal = playerScoreData.NormalBeatData.PerfectBeats + playerScoreData.GoldenBeatData.PerfectBeats;
        return (float)perfectBeatsTotal / totalBeatCount;
    }

    private float CalculateGoodBeatPercent(int totalBeatCount)
    {
        if (totalBeatCount == 0)
        {
            return 0;
        }

        int goodBeatsTotal = playerScoreData.NormalBeatData.GoodBeats + playerScoreData.GoldenBeatData.GoodBeats;
        return (float)goodBeatsTotal / totalBeatCount;
    }

    private string FormatMilliseconds(int millis)
    {
        int seconds = millis / 1000;
        int minutes = seconds / 60;
        int secondsRemaining = seconds - (minutes * 60);
        return string.Format("{0}:{1:00}", minutes, secondsRemaining);
    }
}
