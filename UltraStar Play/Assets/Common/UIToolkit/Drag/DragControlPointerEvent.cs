using UnityEngine;
using UnityEngine.UIElements;

public class DragControlPointerEvent
{
    public Vector3 Position { get; private set; }
    public Vector3 DeltaPosition { get; private set; }
    public int PointerId { get; private set; }
    public int Button { get; private set; }

    public DragControlPointerEvent(IPointerEvent evt)
    {
        Position = evt.position;
        DeltaPosition = evt.deltaPosition;
        PointerId = evt.pointerId;
        Button = evt.button;
    }

    public DragControlPointerEvent(Vector3 position, Vector3 deltaPosition, int pointerId, int button)
    {
        Position = position;
        DeltaPosition = deltaPosition;
        PointerId = pointerId;
        Button = button;
    }
}
