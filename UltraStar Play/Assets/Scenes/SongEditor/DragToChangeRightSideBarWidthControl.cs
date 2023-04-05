using System;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class DragToChangeRightSideBarWidthControl : GeneralDragControl, IDragListener<GeneralDragEvent>
{
    [Inject]
    private CursorManager cursorManager;
    
    [Inject(UxmlName = R.UxmlNames.dragToChangeWidthArea)]
    private VisualElement dragToChangeWidthArea;

    private bool isPointerOverDragToChangeWidthArea;
    private bool canChangeWidth;

    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();
        // Add itself as listener, such that dragging on the VisualElement will move it.
        AddListener(this);
        
        dragToChangeWidthArea.RegisterCallback<PointerEnterEvent>(evt =>
        {
            isPointerOverDragToChangeWidthArea = true;
            canChangeWidth = !IsPointerDown;
            UpdateCursor();
        });
        dragToChangeWidthArea.RegisterCallback<PointerLeaveEvent>(evt =>
        {
            isPointerOverDragToChangeWidthArea = false;
            canChangeWidth = canChangeWidth && IsPointerDown;
            UpdateCursor();
        });
    }

    private void UpdateCursor()
    {
        if (isPointerOverDragToChangeWidthArea)
        {
            cursorManager.SetCursorHorizontal();
        }
        else
        {
            cursorManager.SetDefaultCursor();
        }
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
        UpdateCursor();
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
