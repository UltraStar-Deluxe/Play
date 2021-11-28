using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UIElements;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

/**
 * Fixes the missing scroll support in UIToolkit
 * when clicking inside a ScrollView on non-touch platforms.
 *
 * See https://forum.unity.com/threads/scroll-view-touch-input-on-mobile-scroll-by-drag.945898/
 */
public class MouseEventScrollControl : MonoBehaviour, INeedInjection
{
    [Inject(Optional = true)]
    private UIDocument uiDocument;

    private bool dragging;
    private Vector2 dragStartScrollOffset;
    private Vector2 dragStartPosition;

    public bool invertY = true;

    public void Start()
    {
        if (uiDocument == null)
        {
            return;
        }

        List<ScrollView> scrollViews = uiDocument.rootVisualElement.Query<ScrollView>()
            .ToList();

        scrollViews.ForEach(scrollView =>
        {
            VisualElement contentViewport = scrollView.Q<VisualElement>("unity-content-viewport");

            contentViewport.RegisterCallback<MouseDownEvent>(evt => OnMouseDown(evt, scrollView), TrickleDown.TrickleDown);
            contentViewport.RegisterCallback<MouseMoveEvent>(evt => OnMouseMove(evt, scrollView), TrickleDown.TrickleDown);
            contentViewport.RegisterCallback<MouseUpEvent>(evt => OnMouseUp(), TrickleDown.TrickleDown);
        });
    }

    private void Update()
    {
        // MouseUpEvent is not fired when performed outside of the VisualElement that listens for it.
        // Thus, also check mouse button state here.
        if (dragging
            && Mouse.current != null
            && !Mouse.current.leftButton.isPressed)
        {
            OnMouseUp();
        }
    }

    private void OnMouseDown(MouseDownEvent evt, ScrollView scrollView)
    {
        if (Touchscreen.current != null)
        {
            // Unity itself implements proper scrolling for touch events (just not for mouse events).
            return;
        }

        dragging = true;
        dragStartPosition = evt.localMousePosition;
        dragStartScrollOffset = scrollView.scrollOffset;
    }

    private void OnMouseMove(MouseMoveEvent evt, ScrollView scrollView)
    {
        if (dragging)
        {
            Vector2 dragDelta = evt.localMousePosition - dragStartPosition;
            if (invertY)
            {
                dragDelta = new Vector2(dragDelta.x, -dragDelta.y);
            }
            scrollView.scrollOffset = new Vector2(
                dragStartScrollOffset.x + dragDelta.x,
                dragStartScrollOffset.y + dragDelta.y);
        }
    }

    private void OnMouseUp()
    {
        dragging = false;
    }
}
