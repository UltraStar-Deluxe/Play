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

    private DragControlPointerEvent dragControlPointerDownEvent;
    public ReactiveProperty<EDragState> DragState { get; private set; } = new ReactiveProperty<EDragState>(EDragState.WaitingForPointerDown);

    public VisualElement TargetVisualElement { get; private set; }

    protected readonly GameObject gameObject;

    private PanelHelper panelHelper;

	protected AbstractDragControl(UIDocument uiDocument, VisualElement targetVisualElement, GameObject gameObject)
    {
        this.TargetVisualElement = targetVisualElement;
        this.gameObject = gameObject;
        this.panelHelper = new PanelHelper(uiDocument);
        targetVisualElement.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
        targetVisualElement.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
        targetVisualElement.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);

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

    protected abstract EVENT CreateDragEventStart(DragControlPointerEvent eventData);
    protected abstract EVENT CreateDragEvent(DragControlPointerEvent eventData, EVENT dragStartEvent);

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
        dragControlPointerDownEvent = new DragControlPointerEvent(evt);
        DragState.Value = EDragState.ReadyForDrag;
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (DragState.Value == EDragState.ReadyForDrag)
        {
            Vector2 pointerMoveDistance =  evt.position - dragControlPointerDownEvent.Position;
            if (pointerMoveDistance.magnitude > 5f)
            {
                OnBeginDrag(dragControlPointerDownEvent);
            }
        }
        else if (DragState.Value == EDragState.Dragging)
        {
            OnDrag(new DragControlPointerEvent(evt));
        }
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (DragState.Value == EDragState.Dragging)
        {
            OnEndDrag(new DragControlPointerEvent(evt));
        }

        DragState.Value = EDragState.WaitingForPointerDown;
    }

    private void OnBeginDrag(DragControlPointerEvent eventData)
    {
        if (IsDragging)
        {
            return;
        }

        DragState.Value = EDragState.Dragging;
        dragStartPosition = eventData.Position;
        pointerId = eventData.PointerId;
        dragStartEvent = CreateDragEventStart(eventData);
        NotifyListeners(listener => listener.OnBeginDrag(dragStartEvent), true);
    }

    private void OnDrag(DragControlPointerEvent eventData)
    {
        if (DragState.Value == EDragState.IgnoreDrag
            || !IsDragging
            || eventData.PointerId != pointerId)
        {
            return;
        }

        EVENT dragEvent = CreateDragEvent(eventData, dragStartEvent);
        NotifyListeners(listener => listener.OnDrag(dragEvent), false);
    }

    private void OnEndDrag(DragControlPointerEvent eventData)
    {
        if (DragState.Value != EDragState.Dragging
            || eventData.PointerId != pointerId)
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

    protected GeneralDragEvent CreateGeneralDragEvent(DragControlPointerEvent eventData, GeneralDragEvent localDragStartEvent)
    {
        // Screen coordinates in pixels
        Vector2 screenPosInPixels = eventData.Position;
        Vector2 screenDistanceInPixels = screenPosInPixels - localDragStartEvent.ScreenCoordinateInPixels.StartPosition;
        Vector2 deltaInPixels = eventData.DeltaPosition;

        // Target coordinates in pixels
        float targetWidthInPixels = TargetVisualElement.contentRect.width;
        float targetHeightInPixels = TargetVisualElement.contentRect.height;
        Vector2 targetPosInPixels = new Vector2(TargetVisualElement.resolvedStyle.left, TargetVisualElement.resolvedStyle.top);

        float rectTransformXDistanceInPixels = targetPosInPixels.x - localDragStartEvent.RectTransformCoordinateInPixels.StartPosition.x;
        float rectTransformYDistanceInPixels = targetPosInPixels.y - localDragStartEvent.RectTransformCoordinateInPixels.StartPosition.y;
        Vector2 rectTransformDistanceInPixels = new Vector2(rectTransformXDistanceInPixels, rectTransformYDistanceInPixels);

        GeneralDragEvent result = new GeneralDragEvent(
            new DragCoordinate(
                localDragStartEvent.ScreenCoordinateInPixels.StartPosition,
                screenDistanceInPixels,
                deltaInPixels),
            CreateDragCoordinateInPercent(
                localDragStartEvent.ScreenCoordinateInPixels.StartPosition,
                screenDistanceInPixels,
                deltaInPixels,
                GetReferenceResolution()),
            new DragCoordinate(
                localDragStartEvent.RectTransformCoordinateInPixels.StartPosition,
                rectTransformDistanceInPixels,
                deltaInPixels),
            CreateDragCoordinateInPercent(
                localDragStartEvent.RectTransformCoordinateInPixels.StartPosition,
                rectTransformDistanceInPixels,
                deltaInPixels,
                new Vector2(targetWidthInPixels, targetHeightInPixels)),
            localDragStartEvent.RaycastResultsDragStart,
            localDragStartEvent.InputButton);
        return result;
    }

    protected GeneralDragEvent CreateGeneralDragEventStart(DragControlPointerEvent eventData)
    {
        // Screen coordinate in pixels
        Vector2 screenPosInPixels = eventData.Position;

        // Target coordinate in pixels
        float targetWidthInPixels = TargetVisualElement.contentRect.width;
        float targetHeightInPixels = TargetVisualElement.contentRect.height;
        Vector2 targetPosInPixels = new Vector2(TargetVisualElement.resolvedStyle.left, TargetVisualElement.resolvedStyle.top);

        GeneralDragEvent result = new GeneralDragEvent(
            new DragCoordinate(
                screenPosInPixels,
                Vector2.zero,
                Vector2.zero),
            CreateDragCoordinateInPercent(
                screenPosInPixels,
                Vector2.zero,
                Vector2.zero,
                GetReferenceResolution()),
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
            eventData.Button);
        return result;
    }

    private Vector2 GetReferenceResolution()
    {
        Vector2 screenSizePanelCoordinates = panelHelper.ScreenToPanel(new Vector2(Screen.width, Screen.height));
        return screenSizePanelCoordinates;
    }

    private DragCoordinate CreateDragCoordinateInPercent(Vector2 startPosInPixels, Vector2 distanceInPixels, Vector2 deltaInPixels, Vector2 fullSize)
    {
        Vector2 startPosInPercent = startPosInPixels / fullSize;
        Vector2 distanceInPercent = distanceInPixels / fullSize;
        Vector2 deltaInPercent = deltaInPixels / fullSize;
        return new DragCoordinate(startPosInPercent, distanceInPercent, deltaInPercent);
    }
}
