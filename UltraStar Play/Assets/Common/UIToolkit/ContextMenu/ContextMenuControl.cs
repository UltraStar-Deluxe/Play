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

    private static readonly Vector2 popupOffset = new Vector2(2, 2);

    public Action<ContextMenuPopupControl> FillContextMenuAction { get; set; }

    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();

        InputManager.GetInputAction(R.InputActions.usplay_openContextMenu).PerformedAsObservable()
            .Subscribe(CheckOpenContextMenuFromInputAction)
            .AddTo(gameObject);
    }

    protected virtual void CheckOpenContextMenuFromInputAction(InputAction.CallbackContext context)
    {
        if (Pointer.current == null
            || !context.ReadValueAsButton()
            || IsDragging
            || Touch.activeTouches.Count >= 2
            || targetVisualElement == null
            || uiDocument == null)
        {
            return;
        }

        Vector2 pointerPosition = InputUtils.GetPointerPositionInPanelCoordinates(panelHelper, true);
        if (!targetVisualElement.worldBound.Contains(pointerPosition))
        {
            return;
        }

        OpenContextMenu(pointerPosition + popupOffset);
    }

    public void OpenContextMenu(Vector2 position)
    {
        if (FillContextMenuAction == null)
        {
            return;
        }

        ContextMenuPopupControl contextMenuPopup = new ContextMenuPopupControl(gameObject, position);
        injector.Inject(contextMenuPopup);
        FillContextMenuAction(contextMenuPopup);
    }
}
