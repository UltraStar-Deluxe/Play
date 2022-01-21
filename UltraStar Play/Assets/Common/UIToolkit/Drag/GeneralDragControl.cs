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

public class GeneralDragControl : AbstractDragControl<GeneralDragEvent>
{
    public GeneralDragControl(VisualElement target, GameObject gameObject)
        : base(target, gameObject)
    {
    }

    protected override GeneralDragEvent CreateDragEventStart(DragControlPointerEvent eventData)
    {
        return CreateGeneralDragEventStart(eventData);
    }

    protected override GeneralDragEvent CreateDragEvent(DragControlPointerEvent eventData, GeneralDragEvent dragStartEvent)
    {
        return CreateGeneralDragEvent(eventData, dragStartEvent);
    }
}
