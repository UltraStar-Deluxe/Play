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
    [InjectedInInspector]
    public GameObject coordinateDetailsTextContainer;

    [InjectedInInspector]
    public Text coordinateDetailsText;

    [Inject(searchMethod = SearchMethods.GetComponent)]
    public RectTransform RectTransform { get; private set; }

    public string CoordinateDetails { get; set; } = "";

    public void OnPointerEnter(PointerEventData eventData)
    {
        coordinateDetailsTextContainer.gameObject.SetActive(true);
        coordinateDetailsText.text = CoordinateDetails;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        coordinateDetailsTextContainer.gameObject.SetActive(false);
    }
}
