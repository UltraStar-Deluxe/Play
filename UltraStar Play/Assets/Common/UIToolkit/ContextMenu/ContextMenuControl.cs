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

public class ContextMenuControl : INeedInjection, IInjectionFinishedListener
{
    public const float DragDistanceThreshold = 10f;

    private static readonly Vector2 popupOffset = new Vector2(2, 2);

    public Action<ContextMenuPopupControl> FillContextMenuAction { get; set; }

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    protected VisualElement targetVisualElement;

    [Inject]
    protected UIDocument uiDocument;

    [Inject]
    protected GameObject gameObject;

    [Inject]
    protected Injector injector;

    protected PanelHelper panelHelper;

    public virtual void OnInjectionFinished()
    {
        panelHelper = new PanelHelper(uiDocument);

        InputManager.GetInputAction(R.InputActions.usplay_openContextMenu).PerformedAsObservable()
            .Subscribe(CheckOpenContextMenuFromInputAction)
            .AddTo(gameObject);
    }

    protected virtual void CheckOpenContextMenuFromInputAction(InputAction.CallbackContext context)
    {
        if (Pointer.current == null
            || !context.ReadValueAsButton()
            || Touch.activeTouches.Count >= 2)
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
