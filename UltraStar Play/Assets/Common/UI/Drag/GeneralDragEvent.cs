using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GeneralDragEvent
{
    public int InputButton { get; private set; }

    public List<RaycastResult> RaycastResultsDragStart { get; private set; }

    public DragCoordinate ScreenCoordinateInPixels { get; private set; }
    public DragCoordinate ScreenCoordinateInPercent { get; private set; }
    public DragCoordinate RectTransformCoordinateInPixels { get; private set; }
    public DragCoordinate RectTransformCoordinateInPercent { get; private set; }

    public GeneralDragEvent(
        DragCoordinate screenCoordinateInPixels,
        DragCoordinate screenCoordinateInPercent,
        DragCoordinate rectTransformCoordinateInPixels,
        DragCoordinate rectTransformCoordinateInPercent,
        List<RaycastResult> raycastResultsDragStart,
        int inputButton)
    {
        ScreenCoordinateInPixels = screenCoordinateInPixels;
        ScreenCoordinateInPercent = screenCoordinateInPercent;

        RectTransformCoordinateInPixels = rectTransformCoordinateInPixels;
        RectTransformCoordinateInPercent = rectTransformCoordinateInPercent;

        RaycastResultsDragStart = raycastResultsDragStart;

        InputButton = inputButton;
    }
}
