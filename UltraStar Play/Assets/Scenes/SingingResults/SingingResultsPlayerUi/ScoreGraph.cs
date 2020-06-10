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

public class ScoreGraph : MonoBehaviour, INeedInjection, IExcludeFromSceneInjection
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


}
