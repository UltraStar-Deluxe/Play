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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        anyContextMenuOpenedEventStream = new();
    }

    public Action<ContextMenuPopupControl> FillContextMenuAction { get; set; }
    public Func<bool> ShouldOpenContextMenuFunction { get; set; }

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

    private static Subject<ContextMenuPopupControl> anyContextMenuOpenedEventStream = new();
    public static Subject<ContextMenuPopupControl> AnyContextMenuOpenedEventStream => anyContextMenuOpenedEventStream;

    private readonly Subject<ContextMenuPopupControl> contextMenuOpenedEventStream = new();
    public IObservable<ContextMenuPopupControl> ContextMenuOpenedEventStream => contextMenuOpenedEventStream;

    private readonly Subject<ContextMenuPopupControl> contextMenuClosedEventStream = new();
    public IObservable<ContextMenuPopupControl> ContextMenuClosedEventStream => contextMenuClosedEventStream;

    private VisualElement focusedVisualElementOnOpen;

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

        // Do not open context menu via long press on standalone platform (only with right click)
        if (PlatformUtils.IsStandalone
            && context.control.path == "/Mouse/press")
        {
            return;
        }

        Vector2 pointerPosition = InputUtils.GetPointerPositionInPanelCoordinates(panelHelper, true);
        if (!targetVisualElement.worldBound.Contains(pointerPosition))
        {
            return;
        }
        OpenContextMenu(pointerPosition + popupOffset, null);
    }

    public ContextMenuPopupControl OpenContextMenu(Vector2 position, object context)
    {
        if (FillContextMenuAction == null
            || (ShouldOpenContextMenuFunction != null && !ShouldOpenContextMenuFunction()))
        {
            return null;
        }

        focusedVisualElementOnOpen = targetVisualElement.focusController?.focusedElement as VisualElement;

        ContextMenuPopupControl contextMenuPopupControl = new(gameObject, targetVisualElement, position, context);
        injector.Inject(contextMenuPopupControl);
        FillContextMenuAction(contextMenuPopupControl);

        contextMenuPopupControl.ContextMenuClosedEventStream.Subscribe(_ => OnContextMenuClose(contextMenuPopupControl));
        contextMenuOpenedEventStream.OnNext(contextMenuPopupControl);
        anyContextMenuOpenedEventStream.OnNext(contextMenuPopupControl);
        return contextMenuPopupControl;
    }

    private void OnContextMenuClose(ContextMenuPopupControl contextMenuPopupControl)
    {
        contextMenuClosedEventStream.OnNext(contextMenuPopupControl);

        // Focus last element
        if (focusedVisualElementOnOpen != null)
        {
            focusedVisualElementOnOpen.Focus();
        }
    }

    public void Dispose()
    {
        disposables.ForEach(disposable => disposable.Dispose());
    }
}
