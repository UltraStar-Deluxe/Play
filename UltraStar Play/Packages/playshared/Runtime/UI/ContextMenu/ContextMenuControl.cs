using System;
using System.Collections.Generic;
using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class ContextMenuControl : INeedInjection, IInjectionFinishedListener, IDisposable
{
    private static readonly Vector2 popupOffset = new(2, 2);

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

    private readonly List<IDisposable> disposables = new();

    public virtual void OnInjectionFinished()
    {
        panelHelper = new PanelHelper(uiDocument);
        uiDocument.rootVisualElement.RegisterCallback<PointerDownEvent>(evt => OnPointerDown(evt), TrickleDown.TrickleDown);
        uiDocument.rootVisualElement.RegisterCallback<PointerMoveEvent>(evt => OnPointerMove(evt), TrickleDown.TrickleDown);
        uiDocument.rootVisualElement.RegisterCallback<PointerUpEvent>(evt => OnPointerUp(), TrickleDown.TrickleDown);

        disposables.Add(InputManager.GetInputAction("usplay/openContextMenu").PerformedAsObservable()
            .Subscribe(CheckOpenContextMenuFromInputAction)
            .AddTo(gameObject));
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
        if (distance > InputUtils.DragDistanceThresholdInPx)
        {
            isDrag = true;
        }
    }

    private void OnPointerUp()
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
        Debug.Log($"OpenContextMenu {GetType().Name} {Time.frameCount}");
        OpenContextMenu(pointerPosition + popupOffset);
    }

    public void OpenContextMenu(Vector2 position)
    {
        if (FillContextMenuAction == null)
        {
            return;
        }

        ContextMenuPopupControl contextMenuPopup = new(gameObject, position);
        injector.Inject(contextMenuPopup);
        FillContextMenuAction(contextMenuPopup);
    }

    public void Dispose()
    {
        disposables.ForEach(disposable => disposable.Dispose());
    }
}
