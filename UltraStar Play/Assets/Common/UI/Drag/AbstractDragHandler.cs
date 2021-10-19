using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using PrimeInputActions;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

abstract public class AbstractDragHandler<EVENT> : MonoBehaviour, INeedInjection, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    private readonly List<IDragListener<EVENT>> dragListeners = new List<IDragListener<EVENT>>();

    [Inject]
    public GraphicRaycaster graphicRaycaster;

    public bool IsDragging { get; private set; }
    public Vector2 DragDistance { get; private set; }
    private bool ignoreDrag;

    private EVENT dragStartEvent;
    private int pointerId;

    private Vector2 dragStartPosition;
    
    public RectTransform targetRectTransform;

    protected virtual void Start()
    {
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

    public void AddListener(IDragListener<EVENT> listener)
    {
        dragListeners.Add(listener);
    }

    public void RemoveListener(IDragListener<EVENT> listener)
    {
        dragListeners.Remove(listener);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsDragging)
        {
            return;
        }

        ignoreDrag = false;
        IsDragging = true;
        dragStartPosition = eventData.position;
        pointerId = eventData.pointerId;
        dragStartEvent = CreateDragEventStart(eventData);
        NotifyListeners(listener => listener.OnBeginDrag(dragStartEvent), true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (ignoreDrag
            || !IsDragging
            || eventData.pointerId != pointerId)
        {
            return;
        }

        DragDistance = eventData.position - dragStartPosition;
        EVENT dragEvent = CreateDragEvent(eventData, dragStartEvent);
        NotifyListeners(listener => listener.OnDrag(dragEvent), false);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (ignoreDrag
            || !IsDragging
            || eventData.pointerId != pointerId)
        {
            return;
        }

        EVENT dragEvent = CreateDragEvent(eventData, dragStartEvent);
        NotifyListeners(listener => listener.OnEndDrag(dragEvent), false);
        IsDragging = false;
        DragDistance = Vector2.zero;
    }

    private void CancelDrag()
    {
        if (ignoreDrag)
        {
            return;
        }

        IsDragging = false;
        ignoreDrag = true;
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

    protected abstract EVENT CreateDragEventStart(PointerEventData eventData);
    protected abstract EVENT CreateDragEvent(PointerEventData eventData, EVENT dragStartEvent);

    protected GeneralDragEvent CreateGeneralDragEvent(PointerEventData eventData, GeneralDragEvent dragStartEvent)
    {
        // Screen coordinates in pixels
        Vector2 screenPosInPixels = eventData.position;
        Vector2 screenDistanceInPixels = screenPosInPixels - dragStartEvent.ScreenCoordinateInPixels.StartPosition;
        Vector2 deltaInPixels = eventData.delta;

        // Target RectTransform coordinates in pixels
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetRectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint);

        float rectTransformWidthInPixels = targetRectTransform.rect.width;
        float rectTransformHeightInPixels = targetRectTransform.rect.height;
        Vector2 rectTransformPosInPixels = new Vector2(
            localPoint.x + (rectTransformWidthInPixels / 2),
            localPoint.y + (rectTransformHeightInPixels / 2));

        float rectTransformXDistanceInPixels = rectTransformPosInPixels.x - dragStartEvent.RectTransformCoordinateInPixels.StartPosition.x;
        float rectTransformYDistanceInPixels = rectTransformPosInPixels.y - dragStartEvent.RectTransformCoordinateInPixels.StartPosition.y;
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
                new Vector2(targetRectTransform.rect.width,
                    targetRectTransform.rect.height)),
            dragStartEvent.RaycastResultsDragStart,
            dragStartEvent.InputButton);
        return result;
    }

    protected GeneralDragEvent CreateGeneralDragEventStart(PointerEventData eventData)
    {
        // Screen coordinate in pixels
        Vector2 screenPosInPixels = eventData.pressPosition;

        // Target RectTransform coordinate in pixels
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetRectTransform,
            screenPosInPixels,
            eventData.pressEventCamera,
            out Vector2 localPoint);

        float rectTransformWidthInPixels = targetRectTransform.rect.width;
        float rectTransformHeightInPixels = targetRectTransform.rect.height;
        Vector2 rectTransformPosInPixels = new Vector2(
            localPoint.x + (rectTransformWidthInPixels / 2),
            localPoint.y + (rectTransformHeightInPixels / 2));

        // Raycast
        List<RaycastResult> raycastResults = new List<RaycastResult>();
        PointerEventData eventDataForRaycast = new PointerEventData(EventSystem.current);
        eventDataForRaycast.position = screenPosInPixels;
        graphicRaycaster.Raycast(eventDataForRaycast, raycastResults);

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
                rectTransformPosInPixels,
                Vector2.zero,
                Vector2.zero),
            CreateDragCoordinateInPercent(
                rectTransformPosInPixels,
                Vector2.zero,
                Vector2.zero,
                new Vector2(rectTransformWidthInPixels, rectTransformHeightInPixels)),
            raycastResults,
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
