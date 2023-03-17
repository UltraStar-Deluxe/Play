using System;
using UniRx;
using UnityEngine;

public class DragToMoveControl : GeneralDragControl, IDragListener<GeneralDragEvent>
{
    private Vector2 dragStartPositionInPx;

    private readonly Subject<Vector2> movedEventStream = new();
    public IObservable<Vector2> MovedEventStream => movedEventStream;

    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();
        // Add itself as listener, such that dragging on the VisualElement will move it.
        AddListener(this);
    }

    public void OnBeginDrag(GeneralDragEvent dragEvent)
    {
        dragStartPositionInPx = new Vector2(
            targetVisualElement.resolvedStyle.left,
            targetVisualElement.resolvedStyle.top);
    }

    public void OnDrag(GeneralDragEvent dragEvent)
    {
        Vector2 newPosition = new(
            dragEvent.ScreenCoordinateInPixels.CurrentPosition.x - targetVisualElement.resolvedStyle.width / 2,
            dragEvent.ScreenCoordinateInPixels.CurrentPosition.y - targetVisualElement.resolvedStyle.height / 2);
        MoveTo(newPosition);
    }

    public void OnEndDrag(GeneralDragEvent dragEvent)
    {
        OnDrag(dragEvent);
    }

    public override void CancelDrag()
    {
        base.CancelDrag();
        MoveTo(dragStartPositionInPx);
    }

    public new bool IsCanceled()
    {
        return base.IsCanceled;
    }

    private void MoveTo(Vector2 positionInPx)
    {
        targetVisualElement.style.left = positionInPx.x;
        targetVisualElement.style.top = positionInPx.y;
        movedEventStream.OnNext(positionInPx);
    }
}
