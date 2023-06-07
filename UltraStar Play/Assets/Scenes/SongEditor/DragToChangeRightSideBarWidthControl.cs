using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

public class DragToChangeRightSideBarWidthControl : GeneralDragControl, IDragListener<GeneralDragEvent>
{
    [Inject(UxmlName = R.UxmlNames.dragToChangeWidthArea)]
    private VisualElement dragToChangeWidthArea;

    private bool canChangeWidth;

    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();
        // Add itself as listener, such that dragging on the VisualElement will move it.
        AddListener(this);
        
        dragToChangeWidthArea.RegisterCallback<PointerEnterEvent>(evt =>
        {
            canChangeWidth = !IsPointerDown;
        });
        dragToChangeWidthArea.RegisterCallback<PointerLeaveEvent>(evt =>
        {
            canChangeWidth = canChangeWidth && IsPointerDown;
        });

        CursorManager.SetCursorForVisualElement(dragToChangeWidthArea, ECursor.ArrowsLeftRight);
    }

    public void OnBeginDrag(GeneralDragEvent dragEvent)
    {
        if (!canChangeWidth)
        {
            CancelDrag();
        }
    }

    public void OnDrag(GeneralDragEvent dragEvent)
    {
        ChangeWidthTo(dragEvent.ScreenCoordinateInPixels.CurrentPosition);
    }

    public void OnEndDrag(GeneralDragEvent dragEvent)
    {
        OnDrag(dragEvent);
    }

    protected override void OnPointerUp(IPointerEvent evt)
    {
        base.OnPointerUp(evt);
        canChangeWidth = false;
    }

    public new bool IsCanceled()
    {
        return base.IsCanceled;
    }

    private void ChangeWidthTo(Vector2 positionInPx)
    {
        float currentXMin = targetVisualElement.worldBound.xMin;
        float targetXMin = positionInPx.x;
        float difference = targetXMin - currentXMin;
        if (Mathf.Abs(difference) < 1f)
        {
            return;
        }
        
        targetVisualElement.style.width = targetVisualElement.resolvedStyle.width - difference;
    }
}
