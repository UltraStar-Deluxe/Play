using System;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class DragToMoveControl : GeneralDragControl, IDragListener<GeneralDragEvent>
{
    private Vector2 dragStartPositionInPx;

    private bool isCanceled;

    private readonly Subject<Vector2> movedEventStream = new Subject<Vector2>();
    public IObservable<Vector2> MovedEventStream => movedEventStream;

    public DragToMoveControl(UIDocument uiDocument, VisualElement targetVisualElement, GameObject gameObject)
        : base(uiDocument, targetVisualElement, gameObject)
    {
        // Add itself as listener, such that dragging on the VisualElement will move it.
        AddListener(this);
    }

    public void OnBeginDrag(GeneralDragEvent dragEvent)
    {
        dragStartPositionInPx = new Vector2(
            TargetVisualElement.resolvedStyle.left,
            TargetVisualElement.resolvedStyle.top);
        isCanceled = false;
    }

    public void OnDrag(GeneralDragEvent dragEvent)
    {
        Vector2 newPosition = new Vector2(
            dragEvent.ScreenCoordinateInPixels.CurrentPosition.x - TargetVisualElement.resolvedStyle.width / 2,
            dragEvent.ScreenCoordinateInPixels.CurrentPosition.y - TargetVisualElement.resolvedStyle.height / 2);
        MoveTo(newPosition);
    }

    public void OnEndDrag(GeneralDragEvent dragEvent)
    {
        OnDrag(dragEvent);
    }

    public void CancelDrag()
    {
        MoveTo(dragStartPositionInPx);
        isCanceled = true;
    }

    public bool IsCanceled()
    {
        return isCanceled;
    }

    private void MoveTo(Vector2 positionInPx)
    {
        TargetVisualElement.style.left = positionInPx.x;
        TargetVisualElement.style.top = positionInPx.y;
        movedEventStream.OnNext(positionInPx);
    }
}
