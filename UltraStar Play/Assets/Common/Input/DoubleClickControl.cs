using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class DoubleClickControl
{
    private const float DoubleClickMaxDistanceInPx = 5f;

    private readonly Subject<bool> doublePointerDownEventStream = new();
    public IObservable<bool> DoublePointerDownEventStream => doublePointerDownEventStream;

    private readonly Dictionary<int, float> buttonToLastPointerDownTime = new();
    private readonly Dictionary<int, Vector3> buttonToLastPointerDownPosition = new();

    /**
     * List of buttons that should trigger a double click event.
     * 0 is the left button (and touch), 1 is the right button, 2 is the middle button.
     */
    public List<int> ButtonFilter { get; set; } = new() { 0 };

    // Remember VisualElement for debugging
    private readonly VisualElement visualElement;

    public DoubleClickControl(VisualElement visualElement)
    {
        this.visualElement = visualElement;
        visualElement.RegisterCallback<PointerDownEvent>(evt => OnPointerDown(evt), TrickleDown.TrickleDown);
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (!ButtonFilter.Contains(evt.button))
        {
            return;
        }

        if (buttonToLastPointerDownTime.TryGetValue(evt.button, out float lastPointerDownTime)
            && buttonToLastPointerDownPosition.TryGetValue(evt.button, out Vector3 lastPointerDownPosition))
        {
            bool isDoubleClick = Time.time - lastPointerDownTime < InputUtils.DoubleClickThresholdInSeconds;
            bool isNearLastPosition = Vector3.Distance(evt.position, lastPointerDownPosition) < DoubleClickMaxDistanceInPx;
            if (isDoubleClick && isNearLastPosition)
            {
                doublePointerDownEventStream.OnNext(true);
            }
        }
        buttonToLastPointerDownTime[evt.button] = Time.time;
        buttonToLastPointerDownPosition[evt.button] = evt.position;
    }
}
