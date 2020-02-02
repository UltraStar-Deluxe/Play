using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GeneralDragEvent
{
    public PointerEventData.InputButton InputButton { get; private set; }

    public List<RaycastResult> RaycastResultsDragStart { get; private set; }

    public Vector2 StartPositionInPixels { get; private set; }
    public Vector2 StartPositionInPercent { get; private set; }

    public Vector2 DistanceInPixels { get; private set; }
    public Vector2 DistanceInPercent { get; private set; }

    public GeneralDragEvent(Vector2 dragStartInPixels,
        Vector2 dragStartInPercent,
        Vector2 distanceInPixels,
        Vector2 distanceInPercent,
        List<RaycastResult> raycastResultsDragStart,
        PointerEventData.InputButton inputButton)
    {
        StartPositionInPixels = dragStartInPixels;
        StartPositionInPercent = dragStartInPercent;

        DistanceInPixels = distanceInPixels;
        DistanceInPercent = distanceInPercent;

        RaycastResultsDragStart = raycastResultsDragStart;

        InputButton = inputButton;
    }

}