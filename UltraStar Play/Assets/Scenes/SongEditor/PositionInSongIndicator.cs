using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;

#pragma warning disable CS0649

public class PositionInSongIndicator : MonoBehaviour, INeedInjection
{

    [Inject(searchMethod = SearchMethods.GetComponent)]
    private RectTransform rectTransform;

    public void SetPositionInSongInPercent(double percent)
    {
        float percentFloat = (float)percent;
        rectTransform.anchorMin = new Vector2(percentFloat, rectTransform.anchorMin.y);
        rectTransform.anchorMax = new Vector2(percentFloat, rectTransform.anchorMax.y);
        rectTransform.anchoredPosition = Vector2.zero;
    }
}
