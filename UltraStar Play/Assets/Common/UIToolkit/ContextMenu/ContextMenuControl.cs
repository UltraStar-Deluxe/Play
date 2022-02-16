using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UniRx;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using PrimeInputActions;
using UniRx.Triggers;
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

    private bool isDrag;
    private bool isPointerDown;
    private Vector2 pointerDownPosition;

    public virtual void OnInjectionFinished()
    {
        panelHelper = new PanelHelper(uiDocument);
        targetVisualElement.RegisterCallback<PointerDownEvent>(evt => OnPointerDown(evt));
        targetVisualElement.RegisterCallback<PointerUpEvent>(evt => OnPointerUp(evt));
        targetVisualElement.RegisterCallback<PointerMoveEvent>(evt => OnPointerMove(evt));

        InputManager.GetInputAction(R.InputActions.usplay_openContextMenu).PerformedAsObservable()
            .Subscribe(CheckOpenContextMenuFromInputAction)
            .AddTo(gameObject);
    }

    private void OnPointerDown(IPointerEvent evt)
    {
        isDrag = false;
        isPointerDown = true;
        pointerDownPosition = evt.position;
    }

    private void OnPointerMove(IPointerEvent evt)
    {
        if (!isPointerDown)
        {
            return;
        }

        float distance = Vector2.Distance(evt.position, pointerDownPosition);
        if (distance > DragDistanceThreshold)
        {
            isDrag = true;
        }
    }

    private void OnPointerUp(IPointerEvent evt)
    {
        isPointerDown = false;
        isDrag = false;
    }

    protected virtual void CheckOpenContextMenuFromInputAction(InputAction.CallbackContext context)
    {
        if (Pointer.current == null
            || !context.ReadValueAsButton()
            || Touch.activeTouches.Count >= 2
            || isDrag)
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
