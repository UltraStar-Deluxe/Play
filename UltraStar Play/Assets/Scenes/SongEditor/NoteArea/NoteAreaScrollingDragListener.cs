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

public class NoteAreaScrollingDragListener : MonoBehaviour, INeedInjection, INoteAreaDragListener
{
    [Inject]
    private NoteArea noteArea;

    [Inject]
    private NoteAreaDragHandler noteAreaDragHandler;

    private int viewportXBeginDrag;

    bool isCanceled;

    void Start()
    {
        noteAreaDragHandler.AddListener(this);
    }

    public void OnBeginDrag(NoteAreaDragEvent dragEvent)
    {
        isCanceled = false;
        if (dragEvent.InputButton != PointerEventData.InputButton.Middle)
        {
            CancelDrag();
            return;
        }

        viewportXBeginDrag = noteArea.ViewportX;
    }

    public void OnDrag(NoteAreaDragEvent dragEvent)
    {
        int newViewportX = viewportXBeginDrag - dragEvent.MillisDistance;
        noteArea.SetViewportX(newViewportX);
    }

    public void OnEndDrag(NoteAreaDragEvent dragEvent)
    {
        // Do nothing, scrolling was done already in OnDrag.
    }

    public void CancelDrag()
    {
        isCanceled = true;
    }

    public bool IsCanceled()
    {
        return isCanceled;
    }
}