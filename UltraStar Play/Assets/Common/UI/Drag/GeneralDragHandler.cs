using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.EventSystems;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class GeneralDragHandler : AbstractDragHandler<GeneralDragEvent>
{
    protected override GeneralDragEvent CreateDragEventStart(PointerEventData eventData)
    {
        return CreateGeneralDragEventStart(eventData);
    }

    protected override GeneralDragEvent CreateDragEvent(PointerEventData eventData, GeneralDragEvent dragStartEvent)
    {
        return CreateGeneralDragEvent(eventData, dragStartEvent);
    }
}
