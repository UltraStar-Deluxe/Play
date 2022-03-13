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

public abstract class AbstractDragControl<EVENT> : INeedInjection, IInjectionFinishedListener
{
    private readonly List<IDragListener<EVENT>> dragListeners = new List<IDragListener<EVENT>>();

    public bool IsDragging => DragState.Value == EDragState.Dragging;

    private EVENT dragStartEvent;
    private int pointerId;

    private DragControlPointerEvent dragControlPointerDownEvent;
    public ReactiveProperty<EDragState> DragState { get; private set; } = new ReactiveProperty<EDragState>(EDragState.WaitingForPointerDown);

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    protected VisualElement targetVisualElement;

    [Inject]
    protected UIDocument uiDocument;

    [Inject]
    protected GameObject gameObject;

    protected PanelHelper panelHelper;

    public virtual void OnInjectionFinished()
    {
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

    protected virtual void OnPointerDown(IPointerEvent evt)
    {
        dragControlPointerDownEvent = new DragControlPointerEvent(evt);
        DragState.Value = EDragState.ReadyForDrag;
    }

    protected virtual void OnPointerMove(IPointerEvent evt)
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

    protected virtual void OnPointerUp(IPointerEvent evt)
    {
        if (DragState.Value == EDragState.Dragging)
        {
            OnEndDrag(new DragControlPointerEvent(evt));
        }

        DragState.Value = EDragState.WaitingForPointerDown;
    }

    protected virtual void OnBeginDrag(DragControlPointerEvent eventData)
    {
        if (IsDragging)
        {
            return;
        }

        DragState.Value = EDragState.Dragging;
        pointerId = eventData.PointerId;
        dragStartEvent = CreateDragEventStart(eventData);
        NotifyListeners(listener => listener.OnBeginDrag(dragStartEvent), true);
    }

    protected virtual void OnDrag(DragControlPointerEvent eventData)
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

    protected virtual void OnEndDrag(DragControlPointerEvent eventData)
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

    public virtual void CancelDrag()
    {
        if (DragState.Value == EDragState.IgnoreDrag)
        {
            return;
        }

        DragState.Value = EDragState.IgnoreDrag;
        NotifyListeners(listener => listener.CancelDrag(), false);
    }

    protected void NotifyListeners(Action<IDragListener<EVENT>> action, bool includeCanceledListeners)
    {
        foreach (IDragListener<EVENT> listener in dragListeners.ToList())
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
        Vector2 screenSizeInPanelCoordinates = ApplicationUtils.GetScreenSizeInPanelCoordinates(panelHelper);
        Vector2 screenPosInPixels = new Vector2(
            eventData.Position.x,
            screenSizeInPanelCoordinates.y - eventData.Position.y);
        Vector2 screenDistanceInPixels = screenPosInPixels - localDragStartEvent.ScreenCoordinateInPixels.StartPosition;
        Vector2 deltaInPixels = eventData.DeltaPosition;

        // Target coordinates in pixels
        float targetWidthInPixels = targetVisualElement.worldBound.width;
        float targetHeightInPixels = targetVisualElement.worldBound.height;

        Vector2 localPosInPixels = new Vector2(
            eventData.LocalPosition.x,
            targetHeightInPixels - eventData.LocalPosition.y);
        Vector2 localDistanceInPixels = localPosInPixels - localDragStartEvent.LocalCoordinateInPixels.StartPosition;

        GeneralDragEvent result = new GeneralDragEvent(
            new DragCoordinate(
                localDragStartEvent.ScreenCoordinateInPixels.StartPosition,
                screenDistanceInPixels,
                deltaInPixels),
            CreateDragCoordinateInPercent(
                localDragStartEvent.ScreenCoordinateInPixels.StartPosition,
                screenDistanceInPixels,
                deltaInPixels,
                ApplicationUtils.GetScreenSizeInPanelCoordinates(panelHelper)),
            new DragCoordinate(
                localDragStartEvent.LocalCoordinateInPixels.StartPosition,
                localDistanceInPixels,
                deltaInPixels),
            CreateDragCoordinateInPercent(
                localDragStartEvent.LocalCoordinateInPixels.StartPosition,
                localDistanceInPixels,
                deltaInPixels,
                new Vector2(targetWidthInPixels, targetHeightInPixels)),
            localDragStartEvent.InputButton);
        return result;
    }

    protected GeneralDragEvent CreateGeneralDragEventStart(DragControlPointerEvent eventData)
    {
        // Screen coordinate in pixels
        Vector2 screenSizeInPanelCoordinates = ApplicationUtils.GetScreenSizeInPanelCoordinates(panelHelper);
        Vector2 screenPosInPixels = new Vector2(
            eventData.Position.x,
            screenSizeInPanelCoordinates.y - eventData.Position.y);

        // Target coordinate in pixels
        float targetWidthInPixels = targetVisualElement.contentRect.width;
        float targetHeightInPixels = targetVisualElement.contentRect.height;
        Vector2 localPosInPixels = new Vector2(
            eventData.LocalPosition.x,
            targetHeightInPixels - eventData.LocalPosition.y);

        GeneralDragEvent result = new GeneralDragEvent(
            new DragCoordinate(
                screenPosInPixels,
                Vector2.zero,
                Vector2.zero),
            CreateDragCoordinateInPercent(
                screenPosInPixels,
                Vector2.zero,
                Vector2.zero,
                ApplicationUtils.GetScreenSizeInPanelCoordinates(panelHelper)),
            new DragCoordinate(
                localPosInPixels,
                Vector2.zero,
                Vector2.zero),
            CreateDragCoordinateInPercent(
                localPosInPixels,
                Vector2.zero,
                Vector2.zero,
                new Vector2(targetWidthInPixels, targetHeightInPixels)),
            eventData.Button);
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
