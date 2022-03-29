using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class DoubleClickControl
{
    private readonly Subject<bool> doublePointerDownEventStream = new Subject<bool>();
    public IObservable<bool> DoublePointerDownEventStream => doublePointerDownEventStream;

    private readonly Dictionary<int, float> buttonToLastPointerDownTime = new Dictionary<int, float>();

    /**
     * List of buttons that should trigger a double click event.
     * 0 is the left button (and touch), 1 is the right button, 2 is the middle button.
     */
    public List<int> ButtonFilter { get; set; } = new List<int> { 0 };

    public DoubleClickControl(VisualElement visualElement)
    {
        visualElement.RegisterCallback<PointerDownEvent>(evt => OnPointerDown(evt), TrickleDown.TrickleDown);
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (!ButtonFilter.Contains(evt.button))
        {
            return;
        }

        if (buttonToLastPointerDownTime.TryGetValue(evt.button, out float lastPointerDownTime))
        {
            bool isDoubleClick = Time.time - lastPointerDownTime < InputUtils.DoubleClickThresholdInSeconds;
            if (isDoubleClick)
            {
                doublePointerDownEventStream.OnNext(true);
            }
        }
        buttonToLastPointerDownTime[evt.button] = Time.time;
    }
}
