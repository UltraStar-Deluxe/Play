using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// Helper Component for the Unity Editor to make a RectTransform's position and size follow its anchors.
[ExecuteInEditMode]
[RequireComponent(typeof(RectTransform))]
public class CornersFollowAnchors : MonoBehaviour
{
    public bool manualRefresh;
    public bool followPosition = true;
    public bool followSize = true;

    private Vector2 lastAnchorMin = Vector2.zero;
    private Vector2 lastAnchorMax = Vector2.zero;
    private RectTransform rectTransform;

    void OnEnable()
    {
        rectTransform = GetComponent<RectTransform>();
    }

#if UNITY_EDITOR
    void Update()
    {
        if (manualRefresh)
        {
            manualRefresh = false;
        }
        UpdatePositionFollowAnchors();
    }
#endif

    private void UpdatePositionFollowAnchors()
    {
        if (lastAnchorMin != rectTransform.anchorMin || lastAnchorMax != rectTransform.anchorMax)
        {
            lastAnchorMax = rectTransform.anchorMax;
            lastAnchorMin = rectTransform.anchorMin;
        }
        if (followPosition && rectTransform.anchoredPosition != Vector2.zero)
        {
            rectTransform.anchoredPosition = Vector2.zero;
        }
        if (followSize && rectTransform.sizeDelta != Vector2.zero)
        {
            rectTransform.sizeDelta = Vector2.zero;
        }
    }
}
