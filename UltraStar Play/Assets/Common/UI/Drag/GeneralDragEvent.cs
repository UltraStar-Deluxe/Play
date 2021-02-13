using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GeneralDragEvent
{
    public PointerEventData.InputButton InputButton { get; private set; }

    public List<RaycastResult> RaycastResultsDragStart { get; private set; }

    /**
     * Position where the drag-gesture started.
     */
    public Vector2 StartPositionInPixels { get; private set; }
    public Vector2 StartPositionInPercent { get; private set; }

    /**
     * Distance to the original drag start position.
     */
    public Vector2 DistanceInPixels { get; private set; }
    public Vector2 DistanceInPercent { get; private set; }

    /**
     * Position difference compared to the previous event.
     */
    public Vector2 DragDeltaInPixels { get; private set; }
    
    public GeneralDragEvent(Vector2 dragStartInPixels,
        Vector2 dragStartInPercent,
        Vector2 distanceInPixels,
        Vector2 distanceInPercent,
        Vector2 dragDeltaInPixels,
        List<RaycastResult> raycastResultsDragStart,
        PointerEventData.InputButton inputButton)
    {
        StartPositionInPixels = dragStartInPixels;
        StartPositionInPercent = dragStartInPercent;

        DistanceInPixels = distanceInPixels;
        DistanceInPercent = distanceInPercent;
        DragDeltaInPixels = dragDeltaInPixels;

        RaycastResultsDragStart = raycastResultsDragStart;

        InputButton = inputButton;
    }
}
