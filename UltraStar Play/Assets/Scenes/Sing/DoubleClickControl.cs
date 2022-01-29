using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class DoubleClickControl
{
    private readonly Subject<bool> doublePointerDownEventStream = new Subject<bool>();
    public IObservable<bool> DoublePointerDownEventStream => doublePointerDownEventStream;

    private float lastPointerDownTime;

    public DoubleClickControl(VisualElement visualElement)
    {
        visualElement.RegisterCallback<PointerDownEvent>(evt => OnPointerDown(), TrickleDown.TrickleDown);
    }

    private void OnPointerDown()
    {
        bool isDoubleClick = Time.time - lastPointerDownTime < InputUtils.DoubleClickThresholdInSeconds;
        lastPointerDownTime = Time.time;
        if (isDoubleClick)
        {
            doublePointerDownEventStream.OnNext(true);
        }
    }
}
