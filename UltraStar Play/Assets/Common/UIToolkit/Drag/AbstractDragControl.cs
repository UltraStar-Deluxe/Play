using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public abstract class AbstractDragControl<EVENT>
{
    private readonly List<IDragListener<EVENT>> dragListeners = new List<IDragListener<EVENT>>();

    public bool IsDragging => DragState.Value == EDragState.Dragging;

    private EVENT dragStartEvent;
    private int pointerId;

    private Vector3 dragStartPosition;

    private IPointerEvent pointerDownEvent;
    public ReactiveProperty<EDragState> DragState { get; private set; } = new ReactiveProperty<EDragState>(EDragState.WaitingForPointerDown);

    private readonly VisualElement target;

	public AbstractDragControl(VisualElement target, GameObject gameObject)
    {
        this.target = target;
        target.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
        target.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
        target.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(10)
            .Where(_ => IsDragging)
            .Subscribe(_ =>
            {
                CancelDrag();
                // Cancel other callbacks. To do so, this subscription has a higher priority.
                InputManager.GetInputAction(R.InputActions.usplay_back).CancelNotifyForThisFrame();
            })
            .AddTo(gameObject);
    }

    protected abstract EVENT CreateDragEventStart(IPointerEvent eventData);
    protected abstract EVENT CreateDragEvent(IPointerEvent eventData, EVENT dragStartEvent);

    public void AddListener(IDragListener<EVENT> listener)
    {
        dragListeners.Add(listener);
    }

    public void RemoveListener(IDragListener<EVENT> listener)
    {
        dragListeners.Remove(listener);
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        pointerDownEvent = evt;
        DragState.Value = EDragState.ReadyForDrag;
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (DragState.Value == EDragState.ReadyForDrag)
        {
            Vector2 pointerMoveDistance =  evt.position - pointerDownEvent.position;
            if (pointerMoveDistance.magnitude > 5f)
            {
                OnBeginDrag(pointerDownEvent);
            }
        }
        else if (DragState.Value == EDragState.Dragging)
        {
            OnDrag(evt);
        }
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (DragState.Value == EDragState.Dragging)
        {
            OnEndDrag(evt);
        }

        DragState.Value = EDragState.WaitingForPointerDown;
    }

    private void OnBeginDrag(IPointerEvent eventData)
    {
        if (IsDragging)
        {
            return;
        }

        DragState.Value = EDragState.Dragging;
        dragStartPosition = eventData.position;
        pointerId = eventData.pointerId;
        dragStartEvent = CreateDragEventStart(eventData);
        NotifyListeners(listener => listener.OnBeginDrag(dragStartEvent), true);
    }

    private void OnDrag(IPointerEvent eventData)
    {
        if (DragState.Value == EDragState.IgnoreDrag
            || !IsDragging
            || eventData.pointerId != pointerId)
        {
            return;
        }

        EVENT dragEvent = CreateDragEvent(eventData, dragStartEvent);
        NotifyListeners(listener => listener.OnDrag(dragEvent), false);
    }

    private void OnEndDrag(IPointerEvent eventData)
    {
        if (DragState.Value != EDragState.Dragging
            || eventData.pointerId != pointerId)
        {
            return;
        }

        EVENT dragEvent = CreateDragEvent(eventData, dragStartEvent);
        NotifyListeners(listener => listener.OnEndDrag(dragEvent), false);
        DragState.Value = EDragState.ReadyForDrag;
    }

    private void CancelDrag()
    {
        if (DragState.Value == EDragState.IgnoreDrag)
        {
            return;
        }

        DragState.Value = EDragState.IgnoreDrag;
        NotifyListeners(listener => listener.CancelDrag(), false);
    }

    private void NotifyListeners(Action<IDragListener<EVENT>> action, bool includeCanceledListeners)
    {
        foreach (IDragListener<EVENT> listener in dragListeners)
        {
            if (includeCanceledListeners || !listener.IsCanceled())
            {
                action(listener);
            }
        }
    }

    protected GeneralDragEvent CreateGeneralDragEvent(IPointerEvent eventData, GeneralDragEvent dragStartEvent)
    {
        // Screen coordinates in pixels
        Vector2 screenPosInPixels = eventData.position;
        Vector2 screenDistanceInPixels = screenPosInPixels - dragStartEvent.ScreenCoordinateInPixels.StartPosition;
        Vector2 deltaInPixels = eventData.deltaPosition;

        // Target coordinates in pixels
        float targetWidthInPixels = target.contentRect.width;
        float targetHeightInPixels = target.contentRect.height;
        Vector2 targetPosInPixels = new Vector2(target.resolvedStyle.left, target.resolvedStyle.top);

        float rectTransformXDistanceInPixels = targetPosInPixels.x - dragStartEvent.RectTransformCoordinateInPixels.StartPosition.x;
        float rectTransformYDistanceInPixels = targetPosInPixels.y - dragStartEvent.RectTransformCoordinateInPixels.StartPosition.y;
        Vector2 rectTransformDistanceInPixels = new Vector2(rectTransformXDistanceInPixels, rectTransformYDistanceInPixels);

        GeneralDragEvent result = new GeneralDragEvent(
            new DragCoordinate(
                dragStartEvent.ScreenCoordinateInPixels.StartPosition,
                screenDistanceInPixels,
                deltaInPixels),
            CreateDragCoordinateInPercent(
                dragStartEvent.ScreenCoordinateInPixels.StartPosition,
                screenDistanceInPixels,
                deltaInPixels,
                new Vector2(Screen.width, Screen.height)),
            new DragCoordinate(
                dragStartEvent.RectTransformCoordinateInPixels.StartPosition,
                rectTransformDistanceInPixels,
                deltaInPixels),
            CreateDragCoordinateInPercent(
                dragStartEvent.RectTransformCoordinateInPixels.StartPosition,
                rectTransformDistanceInPixels,
                deltaInPixels,
                new Vector2(targetWidthInPixels, targetHeightInPixels)),
            dragStartEvent.RaycastResultsDragStart,
            dragStartEvent.InputButton);
        return result;
    }

    protected GeneralDragEvent CreateGeneralDragEventStart(IPointerEvent eventData)
    {
        // Screen coordinate in pixels
        Vector2 screenPosInPixels = eventData.position;

        // Target coordinate in pixels
        float targetWidthInPixels = target.contentRect.width;
        float targetHeightInPixels = target.contentRect.height;
        Vector2 targetPosInPixels = new Vector2(target.resolvedStyle.left, target.resolvedStyle.top);

        GeneralDragEvent result = new GeneralDragEvent(
            new DragCoordinate(
                screenPosInPixels,
                Vector2.zero,
                Vector2.zero),
            CreateDragCoordinateInPercent(
                screenPosInPixels,
                Vector2.zero,
                Vector2.zero,
                new Vector2(Screen.width, Screen.height)),
            new DragCoordinate(
                targetPosInPixels,
                Vector2.zero,
                Vector2.zero),
            CreateDragCoordinateInPercent(
                targetPosInPixels,
                Vector2.zero,
                Vector2.zero,
                new Vector2(targetWidthInPixels, targetHeightInPixels)),
            null,
            eventData.button);
        return result;
    }

    private DragCoordinate CreateDragCoordinateInPercent(Vector2 startPosInPixels, Vector2 distanceInPixels, Vector2 deltaInPixels, Vector2 fullSize)
    {
        Vector2 startPosInPercent = startPosInPixels / fullSize;
        Vector2 distanceInPercent = distanceInPixels / fullSize;
        Vector2 deltaInPercent = deltaInPixels / fullSize;
        return new DragCoordinate(startPosInPercent, distanceInPercent, deltaInPercent);
    }
}
