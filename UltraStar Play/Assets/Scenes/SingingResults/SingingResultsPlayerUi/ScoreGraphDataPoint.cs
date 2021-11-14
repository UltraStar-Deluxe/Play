using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.EventSystems;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ScoreGraphDataPoint : MonoBehaviour, INeedInjection, IExcludeFromSceneInjection, IPointerEnterHandler, IPointerExitHandler
{
    [Inject(SearchMethod = SearchMethods.GetComponent)]
    public RectTransform RectTransform { get; private set; }

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private TooltipHandler tooltipHandler;

    public string CoordinateDetails { get; set; } = "";

    public void OnPointerEnter(PointerEventData eventData)
    {
        tooltipHandler.tooltipText = CoordinateDetails;
        tooltipHandler.ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltipHandler.CloseTooltip();
    }
}
