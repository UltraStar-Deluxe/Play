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
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class NoteAreaScrollingDragListener : MonoBehaviour, INeedInjection, IDragListener<NoteAreaDragEvent>
{
    [Inject]
    private NoteArea noteArea;

    [Inject]
    private NoteAreaDragHandler noteAreaDragHandler;

    private int viewportXBeginDrag;
    private int viewportYBeginDrag;

    bool isCanceled;

    private float twoFingerGestureStartDistance;

    private float touchGestureMoveInSameDirectionThreshold = 100f;
    
    void Start()
    {
        noteAreaDragHandler.AddListener(this);
    }

    public void OnBeginDrag(NoteAreaDragEvent dragEvent)
    {
        isCanceled = false;
        if (dragEvent.GeneralDragEvent.InputButton != PointerEventData.InputButton.Middle
            && Touch.activeTouches.Count == 0)
        {
            CancelDrag();
            return;
        }

        twoFingerGestureStartDistance = -1;
        viewportXBeginDrag = noteArea.ViewportX;
        viewportYBeginDrag = noteArea.ViewportY;
    }

    public void OnDrag(NoteAreaDragEvent dragEvent)
    {
        if (dragEvent.GeneralDragEvent.InputButton != PointerEventData.InputButton.Middle
            && Touch.activeTouches.Count < 2)
        {
            return;
        }

        if (Touch.activeTouches.Count == 2)
        {
            if (twoFingerGestureStartDistance < 0)
            {
                twoFingerGestureStartDistance = Vector2.Distance(Touch.activeTouches[0].screenPosition, Touch.activeTouches[1].screenPosition);
            }
            else
            {
                // The touches must all move in the same direction. Thus, their distance to each other must be (near) constant.
                float distance = Vector2.Distance(Touch.activeTouches[0].screenPosition, Touch.activeTouches[1].screenPosition);
                if (Math.Abs(distance - twoFingerGestureStartDistance) > touchGestureMoveInSameDirectionThreshold)
                {
                    CancelDrag();
                    return;
                }
            }
        }
        
        int newViewportX = viewportXBeginDrag - dragEvent.MillisDistance;
        int newViewportY = viewportYBeginDrag - dragEvent.MidiNoteDistance;
        noteArea.SetViewport(newViewportX, newViewportY, noteArea.ViewportWidth, noteArea.ViewportHeight);
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
