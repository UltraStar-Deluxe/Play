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

public abstract class AbstractContextMenuControl : GeneralDragControl, INeedInjection, IInjectionFinishedListener
{
    public const float DragDistanceThreshold = 10f;

    private readonly Vector2 popupOffset = new Vector2(2, 2);

    [Inject]
    protected Injector injector;

    [Inject]
    protected UIDocument uiDocument;

    private PanelHelper panelHelper;

    protected abstract void FillContextMenu(ContextMenuPopupControl contextMenuPopup);

    protected AbstractContextMenuControl(VisualElement targetVisualElement, GameObject gameObject)
        : base(targetVisualElement, gameObject)
    {
        InputManager.GetInputAction(R.InputActions.usplay_openContextMenu).PerformedAsObservable()
            .Subscribe(CheckOpenContextMenuFromInputAction)
            .AddTo(gameObject);
    }

    public void OnInjectionFinished()
    {
        panelHelper = new PanelHelper(uiDocument);
    }

    protected virtual void CheckOpenContextMenuFromInputAction(InputAction.CallbackContext context)
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
        FillContextMenu(contextMenuPopup);
    }
}
