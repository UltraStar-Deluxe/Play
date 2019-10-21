using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RouletteItemPlaceholder : MonoBehaviour
{
    public int renderOrder;

    private RectTransform rectTransform;
    public RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }
            return rectTransform;
        }
    }
}
