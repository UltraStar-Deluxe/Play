using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class NoteAreaSelectionDragListener : MonoBehaviour, INeedInjection, INoteAreaDragListener
{
    [InjectedInInspector]
    public RectTransform selectionFrame;

    [Inject]
    private NoteArea noteArea;

    [Inject]
    private NoteAreaDragHandler noteAreaDragHandler;

    bool isCanceled;

    void Start()
    {
        noteAreaDragHandler.AddListener(this);
    }

    public void OnBeginDrag(NoteAreaDragEvent dragEvent)
    {
        selectionFrame.gameObject.SetActive(true);
        isCanceled = false;
        GameObject raycastTarget = dragEvent.RaycastResultsDragStart.Select(it => it.gameObject).FirstOrDefault();
        if (raycastTarget != noteArea.gameObject)
        {
            CancelDrag();
        }
    }

    public void OnDrag(NoteAreaDragEvent dragEvent)
    {
        UpdateSelectionFrame(dragEvent);
    }

    public void OnEndDrag(NoteAreaDragEvent dragEvent)
    {
        selectionFrame.gameObject.SetActive(false);
    }

    public void CancelDrag()
    {
        selectionFrame.gameObject.SetActive(false);
        isCanceled = true;
    }

    public bool IsCanceled()
    {
        return isCanceled;
    }

    private void UpdateSelectionFrame(NoteAreaDragEvent dragEvent)
    {
        float x = dragEvent.XDragStartInPixels;
        float y = dragEvent.YDragStartInPixels;
        float width = dragEvent.XDistanceInPixels;
        float height = -dragEvent.YDistanceInPixels;

        if (width < 0)
        {
            width = -width;
            x = x - width;
        }
        if (height < 0)
        {
            height = -height;
            y = y + height;
        }
        selectionFrame.position = new Vector2(x, y);
        selectionFrame.sizeDelta = new Vector2(width, height);
    }
}
