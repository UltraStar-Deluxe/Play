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
    public LineRenderer lineRenderer;

    [InjectedInInspector]
    public RectTransform dataPointsContainer;

    [InjectedInInspector]
    public ScoreGraphDataPoint scoreGraphDataPointPrefab;

    [Inject]
    private PlayerScoreControllerData playerScoreData;

    public void OnInjectionFinished()
    {
        InitBeatBars();
        InitDataPoints();
    }

    private void InitBeatBars()
    {
        int totalBeatCount = playerScoreData.NormalNoteLengthTotal + playerScoreData.GoldenNoteLengthTotal;
        float perfectBeatPercent = CalculatePerfectBeatPercent(totalBeatCount);
        float goodBeatPercent = CalculateGoodBeatPercent(totalBeatCount);

        PositionBeatBar(perfectBeatBar, 0, perfectBeatPercent);
        PositionBeatBar(goodBeatBar, perfectBeatPercent, perfectBeatPercent + goodBeatPercent);
        PositionBeatBar(missedBeatBar, perfectBeatPercent + goodBeatPercent, totalBeatCount > 0 ? 1 : 0);
    }

    private void InitDataPoints()
    {
        // Remove dummy data points
        foreach (ScoreGraphDataPoint dataPoint in dataPointsContainer.GetComponentsInChildren<ScoreGraphDataPoint>())
        {
            Destroy(dataPoint.gameObject);
        }
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
}
