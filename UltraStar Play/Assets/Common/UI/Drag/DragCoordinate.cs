using UnityEngine;

public class DragCoordinate
{
    public Vector2 StartPosition { get; private set; }
    public Vector2 Distance { get; private set; }
    public Vector2 CurrentPosition => StartPosition + Distance;

    /**
     * Position difference compared to the previous event.
     */
    public Vector2 DragDelta { get; private set; }

    public DragCoordinate(
        Vector2 startPosition,
        Vector2 distance,
        Vector2 dragDelta)
    {
        StartPosition = startPosition;
        Distance = distance;
        DragDelta = dragDelta;
    }
}
