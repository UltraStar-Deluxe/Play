using UnityEngine;
using UnityEngine.UIElements;

public class DragControlPointerEvent
{
    public Vector3 Position { get; private set; }
    public Vector3 LocalPosition { get; private set; }
    public Vector3 DeltaPosition { get; private set; }
    public int PointerId { get; private set; }
    public int Button { get; private set; }

    public DragControlPointerEvent(IPointerEvent evt)
    {
        Position = evt.position;
        LocalPosition = evt.localPosition;
        DeltaPosition = evt.deltaPosition;
        PointerId = evt.pointerId;
        Button = evt.button;
    }
    
    public DragControlPointerEvent(IPointerEvent evt, Vector3 offset)
    {
        Position = evt.position + offset;
        LocalPosition = evt.localPosition + offset;
        DeltaPosition = evt.deltaPosition;
        PointerId = evt.pointerId;
        Button = evt.button;
    }
}
