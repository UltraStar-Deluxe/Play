using UniInject;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

/**
 * Fixes the missing scroll support in UIToolkit
 * when clicking inside a ScrollView on non-touch platforms.
 *
 * See https://forum.unity.com/threads/scroll-view-touch-input-on-mobile-scroll-by-drag.945898/
 */
public class MouseEventScrollControl : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private UIDocument uiDocument;

    private bool dragging;
    private Vector2 dragStartScrollOffset;
    private Vector2 dragStartPosition;
    private ScrollView mouseDownScrollView;

    public void OnInjectionFinished()
    {
        DoRegisterMouseScrollEvents();
    }

    public static void RegisterMouseScrollEvents()
    {
        MouseEventScrollControl mouseEventScrollControl = FindObjectOfType<MouseEventScrollControl>();
        if (mouseEventScrollControl == null)
        {
            return;
        }
        mouseEventScrollControl.DoRegisterMouseScrollEvents();
    }
    
    private void DoRegisterMouseScrollEvents()
    {
        VisualElement rootVisualElement = uiDocument.rootVisualElement;
        rootVisualElement.RegisterCallback<MouseMoveEvent>(evt => OnMouseMoveOnRootVisualElement(evt), TrickleDown.TrickleDown);
        rootVisualElement.RegisterCallback<MouseDownEvent>(evt => OnMouseDownOnRootVisualElement(evt), TrickleDown.TrickleDown);
        rootVisualElement.RegisterCallback<MouseUpEvent>(evt => OnMouseUpOnRootVisualElement(), TrickleDown.TrickleDown);
    }

    private void Update()
    {
        // MouseUpEvent is not fired when performed outside of the VisualElement that listens for it.
        // Thus, also check mouse button state here.
        if (dragging
            && Mouse.current != null
            && !Mouse.current.leftButton.isPressed)
        {
            OnMouseUpOnRootVisualElement();
        }
    }

    private void OnMouseDownOnRootVisualElement(MouseDownEvent evt)
    {
        if (Touchscreen.current != null)
        {
            // Unity itself implements proper scrolling for touch events (just not for mouse events).
            return;
        }

        if (evt.target is not VisualElement visualElement)
        {
            return;
        }

        if (visualElement is ScrollView scrollView)
        {
            mouseDownScrollView = scrollView;
        }
        else
        {
            mouseDownScrollView = visualElement.GetParent(parent => parent is ScrollView) as ScrollView;
        }

        if (mouseDownScrollView == null)
        {
            return;
        }
        
        dragging = true;
        dragStartPosition = evt.localMousePosition;
        dragStartScrollOffset = mouseDownScrollView.scrollOffset;
    }

    private void OnMouseMoveOnRootVisualElement(MouseMoveEvent evt)
    {
        if (dragging
            && !(uiDocument.rootVisualElement.focusController.focusedElement is TextField))
        {
            Vector2 dragDelta = dragStartPosition - evt.localMousePosition;
            mouseDownScrollView.scrollOffset = new Vector2(
                dragStartScrollOffset.x + dragDelta.x,
                dragStartScrollOffset.y + dragDelta.y);
        }
    }

    private void OnMouseUpOnRootVisualElement()
    {
        mouseDownScrollView = null;
        dragging = false;
    }
}
