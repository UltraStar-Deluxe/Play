using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UnityEngine.InputSystem;

public class ContextMenu : AbstractPointerSensitivePopup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        OpenContextMenus = new List<ContextMenu>();
    }
    
    public ContextMenuItem contextMenuItemPrefab;
    public ContextMenuSeparator contextMenuSeparatorPrefab;

    private readonly List<IDisposable> disposables = new List<IDisposable>();

    private bool wasNoButtonOrTouchPressed;

    public static List<ContextMenu> OpenContextMenus { get; private set; } = new List<ContextMenu>();
    public static bool IsAnyContextMenuOpen => OpenContextMenus.Count > 0;
    
    protected override void Awake()
    {
        base.Awake();

        // Destroy dummy items from prefab
        transform.DestroyAllDirectChildren();
        
        // Close with next click or tap
        disposables.Add(InputManager.GetInputAction(R.InputActions.usplay_closeContextMenu).PerformedAsObservable()
            .Subscribe(context =>
            {
                // Only close when the mouse / touchscreen has been fully released in the mean time.
                if (!wasNoButtonOrTouchPressed)
                {
                    return;
                }
                
                // Do not close when clicking an item
                Vector2 position = new Vector2(Pointer.current.position.x.ReadValue(), Pointer.current.position.y.ReadValue());
                if (RectTransformUtility.RectangleContainsScreenPoint(RectTransform, position))
                {
                    return;
                }
                
                CloseContextMenu();
            }));
    }

    private void Start()
    {
        OpenContextMenus.Add(this);
    }

    void Update()
    {
        wasNoButtonOrTouchPressed = wasNoButtonOrTouchPressed
                              || !InputUtils.AnyKeyboardOrMouseOrTouchPressed();
    }
    
    public ContextMenuSeparator AddSeparator()
    {
        ContextMenuSeparator contextMenuSeparator = Instantiate(contextMenuSeparatorPrefab, this.transform);
        return contextMenuSeparator;
    }

    public ContextMenuItem AddItem(string label, Action action)
    {
        ContextMenuItem contextMenuItem = Instantiate(contextMenuItemPrefab, this.transform);
        contextMenuItem.Text = label;
        contextMenuItem.ContextMenu = this;
        contextMenuItem.SetAction(action);
        return contextMenuItem;
    }

    public void CloseContextMenu()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        disposables.ForEach(it => it.Dispose());
        
        // Remove this ContextMenu from the list of opened ContextMenus only after all Input has been released
        // to avoid triggering additional actions (e.g. onClick of button).
        CoroutineManager.Instance.StartCoroutineAlsoForEditor(
            CoroutineUtils.ExecuteWhenConditionIsTrue(
                () => !InputUtils.AnyKeyboardOrMouseOrTouchPressed(),
                () => OpenContextMenus.Remove(this)));
    }
}
