using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UnityEngine.InputSystem;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class ContextMenu : AbstractPointerSensitivePopup
{
    public ContextMenuItem contextMenuItemPrefab;
    public ContextMenuSeparator contextMenuSeparatorPrefab;

    private readonly List<IDisposable> disposables = new List<IDisposable>();

    private bool wasNoButtonOrTouchPressed;
    
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

    void Update()
    {
        wasNoButtonOrTouchPressed = wasNoButtonOrTouchPressed
                              || !(AnyMouseButtonPressed() || AnyTouchscreenPressed() || AnyKeyboardButtonPressed());
    }

    public bool AnyKeyboardButtonPressed()
    {
        return Keyboard.current != null
               && Keyboard.current.anyKey.ReadValue() > 0;
    }

    public bool AnyMouseButtonPressed()
    {
        return Mouse.current != null
               && (Mouse.current.leftButton.isPressed
                   || Mouse.current.rightButton.isPressed
                   || Mouse.current.middleButton.isPressed);
    }
    
    public bool AnyTouchscreenPressed()
    {
        return Touchscreen.current != null
               && Touchscreen.current.touches.Count > 0;
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
    }
}
