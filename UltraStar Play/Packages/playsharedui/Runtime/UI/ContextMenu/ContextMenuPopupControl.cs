using System;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class ContextMenuPopupControl : INeedInjection, IInjectionFinishedListener
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        openContextMenuPopups = new List<ContextMenuPopupControl>();
        anyContextMenuOpenedEventStream = new Subject<ContextMenuOpenedEvent>();
    }

    private bool wasNoButtonOrTouchPressed;

    public static IReadOnlyList<ContextMenuPopupControl> OpenContextMenuPopups => openContextMenuPopups;
    private static List<ContextMenuPopupControl> openContextMenuPopups = new();

    public static IObservable<ContextMenuOpenedEvent> AnyContextMenuOpenedEventStream => anyContextMenuOpenedEventStream;
    private static Subject<ContextMenuOpenedEvent> anyContextMenuOpenedEventStream = new Subject<ContextMenuOpenedEvent>();

    public static bool IsAnyContextMenuPopupOpen => OpenContextMenuPopups.Count > 0;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private ContextMenuPopupManager contextMenuPopupManager;

    [Inject]
    private Injector injector;

    private readonly VisualElement targetElement;
    public VisualElement TargetElement => targetElement;

    private PanelHelper panelHelper;

    private VisualElement visualElement;
    public VisualElement VisualElement => visualElement;

    private readonly GameObject gameObject;
    private readonly Vector2 position;
    public Vector2 Position => position;

    private IDisposable closeContextMenuDisposable;

    private Vector2 lastSize;
    private Vector2 lastPosition;

    public IObservable<ContextMenuOpenedEvent> ContextMenuOpenedEventStream => contextMenuOpenedEventStream;
    private readonly Subject<ContextMenuOpenedEvent> contextMenuOpenedEventStream = new Subject<ContextMenuOpenedEvent>();

    public IObservable<ContextMenuClosedEvent> ContextMenuClosedEventStream => contextMenuClosedEventStream;
    private readonly Subject<ContextMenuClosedEvent> contextMenuClosedEventStream = new Subject<ContextMenuClosedEvent>();

    /**
     * Optional object to associate data with the popup menu.
     */
    private readonly object context;
    public object Context => context; // Public getter to allow modding

    public ContextMenuPopupControl(
        GameObject gameObject,
        VisualElement targetElement,
        Vector2 position,
        object context)
    {
        this.gameObject = gameObject;
        this.targetElement = targetElement;
        this.position = position;
        this.context = context;
    }

    public void OnInjectionFinished()
    {
        panelHelper = new PanelHelper(uiDocument);
        visualElement = contextMenuPopupManager.contextMenuUi.CloneTree().Children().First();
        visualElement.style.left = position.x;
        visualElement.style.top = position.y;
        uiDocument.rootVisualElement.Add(visualElement);
        // Remove dummy items
        visualElement.Clear();

        // Close with next click or tap
        closeContextMenuDisposable = InputManager.GetInputAction("usplay/closeContextMenu").PerformedAsObservable()
            .Subscribe(context => OnCloseContextMenu(context));
        closeContextMenuDisposable.AddTo(gameObject);

        CloseAllOpenContextMenus();
        openContextMenuPopups.Add(this);
        contextMenuOpenedEventStream.OnNext(new ContextMenuOpenedEvent(this));
        anyContextMenuOpenedEventStream.OnNext(new ContextMenuOpenedEvent(this));

        visualElement.RegisterCallback<GeometryChangedEvent>(evt =>
        {
            Vector2 currentSize = new(visualElement.resolvedStyle.width, visualElement.resolvedStyle.height);
            Vector2 currentPosition = new(visualElement.resolvedStyle.left, visualElement.resolvedStyle.top);
            if (currentSize != lastSize
                || currentPosition != lastPosition)
            {
                VisualElementUtils.MoveVisualElementFullyInsideScreen(visualElement, panelHelper);
            }

            lastSize = currentSize;
            lastPosition = currentPosition;
        });
    }

    private void OnCloseContextMenu(InputAction.CallbackContext context)
    {
        // Only close when the mouse / touchscreen has been fully released in the mean time.
        if (!wasNoButtonOrTouchPressed
            || !context.ReadValueAsButton())
        {
            return;
        }

        // Do not close when clicking an item
        Vector2 pointerPosition = InputUtils.GetPointerPositionInPanelCoordinates(panelHelper, true);
        if (visualElement.worldBound.Contains(pointerPosition))
        {
            return;
        }

        CloseContextMenu();
    }

    public void Update()
    {
        wasNoButtonOrTouchPressed = wasNoButtonOrTouchPressed || !InputUtils.AnyKeyboardOrMouseOrTouchPressed();
    }

    public VisualElement AddSeparator()
    {
        VisualElement separator = contextMenuPopupManager.contextMenuSeparatorUi.CloneTree().Children().First();
        visualElement.Add(separator);
        return separator;
    }

    public VisualElement AddVisualElement(VisualElement newVisualElement)
    {
        visualElement.Add(newVisualElement);
        return newVisualElement;
    }

    public VisualElement AddButton(Translation text, Action action)
    {
        return AddButton(text, null, action);
    }

    public VisualElement AddButton(Translation text, string icon, Action action)
    {
        return AddButton(text, icon, "", action);
    }

    public VisualElement AddButton(Translation text, string icon, string name, Action action)
    {
        VisualElement contextMenuItemVisualElement = contextMenuPopupManager.contextMenuItemUi.CloneTree().Children().First();
        ContextMenuItemControl contextMenuItemControl = new(text, icon, name, action);
        contextMenuItemControl.ItemTriggeredEventStream.Subscribe(evt => CloseContextMenu());
        injector.WithRootVisualElement(contextMenuItemVisualElement).Inject(contextMenuItemControl);
        visualElement.Add(contextMenuItemVisualElement);
        return contextMenuItemVisualElement;
    }

    public void CloseContextMenu()
    {
        closeContextMenuDisposable.Dispose();
        visualElement.RemoveFromHierarchy();
        openContextMenuPopups.Remove(this);
        contextMenuClosedEventStream.OnNext(new ContextMenuClosedEvent(this));
    }

    private static void CloseAllOpenContextMenus()
    {
        OpenContextMenuPopups.ToList().ForEach(contextMenu => contextMenu.CloseContextMenu());
    }
}
