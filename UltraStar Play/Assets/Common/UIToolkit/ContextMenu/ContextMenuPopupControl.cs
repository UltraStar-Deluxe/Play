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
    static void Init()
    {
        OpenContextMenuPopups = new List<ContextMenuPopupControl>();
    }
    
    private bool wasNoButtonOrTouchPressed;

    public static List<ContextMenuPopupControl> OpenContextMenuPopups { get; private set; } = new List<ContextMenuPopupControl>();
    public static bool IsAnyContextMenuPopupOpen => OpenContextMenuPopups.Count > 0;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private Injector injector;

    private PanelHelper panelHelper;

    private VisualElement visualElement;

    private readonly GameObject gameObject;
    private readonly Vector2 position;

    private IDisposable closeContextMenuDisposable;

    private Vector2 lastSize;
    private Vector2 lastPosition;

    public ContextMenuPopupControl(GameObject gameObject, Vector2 position)
    {
        this.gameObject = gameObject;
        this.position = position;
    }

    public void OnInjectionFinished()
    {
        panelHelper = new PanelHelper(uiDocument);
        visualElement = uiManager.contextMenuUi.CloneTree().Children().First();
        visualElement.style.left = position.x;
        visualElement.style.top = position.y;
        uiDocument.rootVisualElement.Children().First().Add(visualElement);
        // Remove dummy items
        visualElement.Clear();

        // Close with next click or tap
        closeContextMenuDisposable = InputManager.GetInputAction(R.InputActions.usplay_closeContextMenu).PerformedAsObservable()
            .Subscribe(context => OnCloseContextMenu(context));
        closeContextMenuDisposable.AddTo(gameObject);

        CloseAllOpenContextMenus();
        OpenContextMenuPopups.Add(this);

        visualElement.RegisterCallback<GeometryChangedEvent>(evt =>
        {
            Vector2 currentSize = new Vector2(visualElement.resolvedStyle.width, visualElement.resolvedStyle.height);
            Vector2 currentPosition = new Vector2(visualElement.resolvedStyle.left, visualElement.resolvedStyle.top);
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
    
    public void AddSeparator()
    {
        VisualElement contextMenuItemVisualElement = uiManager.contextMenuSeparatorUi.CloneTree().Children().First();
        visualElement.Add(contextMenuItemVisualElement);
    }

    public void AddItem(string text, Action action)
    {
        VisualElement contextMenuItemVisualElement = uiManager.contextMenuItemUi.CloneTree().Children().First();
        ContextMenuItemControl contextMenuItem = new ContextMenuItemControl(text, action);
        contextMenuItem.ItemTriggeredEventStream.Subscribe(evt => CloseContextMenu());
        injector.WithRootVisualElement(contextMenuItemVisualElement).Inject(contextMenuItem);
        visualElement.Add(contextMenuItemVisualElement);
    }

    public void CloseContextMenu()
    {
        closeContextMenuDisposable.Dispose();
        visualElement.RemoveFromHierarchy();
        OpenContextMenuPopups.Remove(this);
    }

    private static void CloseAllOpenContextMenus()
    {
        OpenContextMenuPopups.ToList().ForEach(contextMenu => contextMenu.CloseContextMenu());
    }
}
