using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UniRx.Triggers;
using UnityEngine.EventSystems;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class RectTransformSlideIntoViewport : MonoBehaviour, INeedInjection
{
    public enum Side
    {
        Left, Right, Top, Bottom
    }

    public RectTransform triggerArea;

    public Side side;

    public float animTimeInSeconds = 0.2f;

    [Inject(searchMethod = SearchMethods.GetComponent)]
    private RectTransform rectTransform;

    private Vector2 insideAnchorMin;
    private Vector2 insideAnchorMax;

    private Vector2 outsideAnchorMin;
    private Vector2 outsideAnchorMax;

    private bool isInside;
    private bool mouseOverTriggerArea;

    void Start()
    {
        // Find inside and outside positions
        insideAnchorMin = rectTransform.anchorMin;
        insideAnchorMax = rectTransform.anchorMax;

        Vector2 outsideAnchorOffset = GetOutsideAnchorOffset(side);
        outsideAnchorMin = insideAnchorMin + outsideAnchorOffset;
        outsideAnchorMax = insideAnchorMax + outsideAnchorOffset;

        // Start outside
        rectTransform.anchorMin = outsideAnchorMin;
        rectTransform.anchorMax = outsideAnchorMax;
        rectTransform.MoveCornersToAnchors();

        // Slide in when mouse over trigger area
        PointerEnterExitListener triggerAreaPointerEnterExitListener = GameObjectUtils.GetOrAddComponent<PointerEnterExitListener>(triggerArea.gameObject);
        triggerAreaPointerEnterExitListener.onEnterAction = _ =>
        {
            mouseOverTriggerArea = true;
            SlideIn();
        };
        triggerAreaPointerEnterExitListener.onExitAction = _ =>
        {
            mouseOverTriggerArea = false;
        };
    }

    void Update()
    {
        // Slide out when mouse not over trigger area and not over this RectTransform itself
        if (isInside
            && !mouseOverTriggerArea
            && !RectTransformUtils.IsMouseOverRectTransform(rectTransform))
        {
            SlideOut();
        }
    }

    private Vector2 GetOutsideAnchorOffset(Side theSide)
    {
        Vector2 anchorSize = new Vector2(
            rectTransform.anchorMax.x - rectTransform.anchorMin.x,
            rectTransform.anchorMax.y - rectTransform.anchorMin.y);
        return GetDirectionVector(theSide) * anchorSize;
    }

    private Vector2 GetDirectionVector(Side theSide)
    {
        switch (theSide)
        {
            case Side.Left:
                return new Vector2(-1, 0);
            case Side.Right:
                return new Vector2(1, 0);
            case Side.Top:
                return new Vector2(0, 1);
            case Side.Bottom:
                return new Vector2(0, -1);
            default:
                // Use return at end of method
                break;
        }
        return Vector2.zero;
    }

    public void SlideOut()
    {
        isInside = false;
        SlideTo(outsideAnchorMin, outsideAnchorMax);
    }

    public void SlideIn()
    {
        isInside = true;
        SlideTo(insideAnchorMin, insideAnchorMax);
    }

    private void SlideTo(Vector2 targetAnchorMin, Vector2 targetAnchorMax)
    {
        LeanTween.value(gameObject, rectTransform.anchorMin, targetAnchorMin, animTimeInSeconds)
            .setOnUpdate((Vector2 val) =>
            {
                rectTransform.anchorMin = val;
                rectTransform.MoveCornersToAnchors();
            });
        LeanTween.value(gameObject, rectTransform.anchorMax, targetAnchorMax, animTimeInSeconds)
            .setOnUpdate((Vector2 val) =>
            {
                rectTransform.anchorMax = val;
                rectTransform.MoveCornersToAnchors();
            });
    }
}
