using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UniRx;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using PrimeInputActions;
using UnityEngine.UIElements;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class ContextMenuControl : GeneralDragControl
{
    public const float DragDistanceThreshold = 10f;

    private readonly Vector2 popupOffset = new Vector2(2, 2);

    private readonly Action<ContextMenuPopupControl> fillContextMenuAction;

    private readonly Injector injector;

    public ContextMenuControl(UIDocument uiDocument,
        VisualElement targetVisualElement,
        GameObject gameObject,
        Injector injector,
        Action<ContextMenuPopupControl> fillContextMenuAction)
        : base(uiDocument, targetVisualElement, gameObject)
    {
        InputManager.GetInputAction(R.InputActions.usplay_openContextMenu).PerformedAsObservable()
            .Subscribe(CheckOpenContextMenuFromInputAction)
            .AddTo(gameObject);
        this.fillContextMenuAction = fillContextMenuAction;
        this.injector = injector;
    }

    protected void CheckOpenContextMenuFromInputAction(InputAction.CallbackContext context)
    {
        if (Pointer.current == null
            || !context.ReadValueAsButton()
            || IsDragging
            || Touch.activeTouches.Count >= 2
            || TargetVisualElement == null
            || uiDocument == null)
        {
            return;
        }

        Vector2 pointerPosition = InputUtils.GetPointerPositionInPanelCoordinates(panelHelper, true);
        if (!TargetVisualElement.worldBound.Contains(pointerPosition))
        {
            return;
        }

        OpenContextMenu(pointerPosition + popupOffset);
    }

    public void OpenContextMenu(Vector2 position)
    {
        ContextMenuPopupControl contextMenuPopup = new ContextMenuPopupControl(gameObject, position);
        injector.Inject(contextMenuPopup);
        fillContextMenuAction(contextMenuPopup);
    }

    private static void DefaultFillContextMenuAction(ContextMenuPopupControl contextMenuPopupControl)
    {
        // Do nothing.
    }
}
