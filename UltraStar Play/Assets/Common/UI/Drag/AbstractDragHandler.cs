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

    abstract protected EVENT CreateDragEventStart(PointerEventData eventData);
    abstract protected EVENT CreateDragEvent(PointerEventData eventData, EVENT dragStartEvent);

    protected GeneralDragEvent CreateGeneralDragEvent(PointerEventData eventData, GeneralDragEvent dragStartEvent)
    {
        float xDistanceInPixels = eventData.position.x - dragStartEvent.StartPositionInPixels.x;
        float yDistanceInPixels = eventData.position.y - dragStartEvent.StartPositionInPixels.y;
        Vector2 distanceInPixels = new Vector2(xDistanceInPixels, yDistanceInPixels);
        
        float widthInPixels = targetRectTransform.rect.width;
        float heightInPixels = targetRectTransform.rect.height;
        float xDistanceInPercent = xDistanceInPixels / widthInPixels;
        float yDistanceInPercent = yDistanceInPixels / heightInPixels;
        Vector2 distanceInPercent = new Vector2(xDistanceInPercent, yDistanceInPercent);

        GeneralDragEvent result = new GeneralDragEvent(dragStartEvent.StartPositionInPixels,
            dragStartEvent.StartPositionInPercent,
            distanceInPixels,
            distanceInPercent,
            eventData.delta,
            dragStartEvent.RaycastResultsDragStart,
            dragStartEvent.InputButton);
        return result;
    }

    protected GeneralDragEvent CreateGeneralDragEventStart(PointerEventData eventData)
    {
        float xDragStartInPixels = eventData.pressPosition.x;
        float yDragStartInPixels = eventData.pressPosition.y;
        Vector2 dragStartInPixels = new Vector2(xDragStartInPixels, yDragStartInPixels);
        

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        PointerEventData eventDataForRaycast = new PointerEventData(EventSystem.current);
        eventDataForRaycast.position = eventData.pressPosition;
        graphicRaycaster.Raycast(eventDataForRaycast, raycastResults);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(targetRectTransform,
                                                                eventData.pressPosition,
                                                                eventData.pressEventCamera,
                                                                out Vector2 localPoint);

        float widthInPixels = targetRectTransform.rect.width;
        float heightInPixels = targetRectTransform.rect.height;
        float xDragStartInPercent = (localPoint.x + (widthInPixels / 2)) / widthInPixels;
        float yDragStartInPercent = (localPoint.y + (heightInPixels / 2)) / heightInPixels;
        Vector2 dragStartInPercent = new Vector2(xDragStartInPercent, yDragStartInPercent);

        Vector2 distanceInPixels = Vector2.zero;
        Vector2 distanceInPercent = Vector2.zero;

        GeneralDragEvent result = new GeneralDragEvent(dragStartInPixels,
            dragStartInPercent,
            distanceInPixels,
            distanceInPercent,
            Vector2.zero, 
            raycastResults,
            eventData.button);
        return result;
    }
}
