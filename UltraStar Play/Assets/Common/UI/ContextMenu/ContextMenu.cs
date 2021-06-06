using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UniRx;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using PrimeInputActions;

public class ContextMenu : AbstractPointerSensitivePopup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        OpenContextMenus = new List<ContextMenu>();
    }
    
    public ContextMenuItem contextMenuItemPrefab;
    public ContextMenuSeparator contextMenuSeparatorPrefab;

    private bool wasNoButtonOrTouchPressed;

    public static List<ContextMenu> OpenContextMenus { get; private set; } = new List<ContextMenu>();
    public static bool IsAnyContextMenuOpen => OpenContextMenus.Count > 0;

    private static Canvas canvas;
    private static Canvas Canvas
    {
        get
        {
            if (canvas == null)
            {
                canvas = CanvasUtils.FindCanvas();
            }
            return canvas;
        }
    }

    private bool isMovedIntoCanvas;
    
    protected override void Awake()
    {
        base.Awake();

        // Destroy dummy items from prefab
        transform.DestroyAllDirectChildren();
        
        // Close with next click or tap
        InputManager.GetInputAction(R.InputActions.usplay_closeContextMenu).PerformedAsObservable()
            .Subscribe(context =>
            {
                // Only close when the mouse / touchscreen has been fully released in the mean time.
                if (!wasNoButtonOrTouchPressed
                    || !context.ReadValueAsButton())
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
            })
            .AddTo(gameObject);
    }

    private void Start()
    {
        OpenContextMenus.Add(this);
    }

    void Update()
    {
        wasNoButtonOrTouchPressed = wasNoButtonOrTouchPressed
                              || !InputUtils.AnyKeyboardOrMouseOrTouchPressed();

        if (!isMovedIntoCanvas
            && RectTransform.rect.width > 0
            && RectTransform.rect.height > 0)
        {
            isMovedIntoCanvas = true;
            MoveInsideCanvas();
        }
    }
    
    public ContextMenuSeparator AddSeparator()
    {
        ContextMenuSeparator contextMenuSeparator = Instantiate(contextMenuSeparatorPrefab, this.transform);
        return contextMenuSeparator;
    }

    public ContextMenuItem AddItem(string label, Action action)
    {
        // Could be out of screen
        isMovedIntoCanvas = false;
        
        ContextMenuItem contextMenuItem = Instantiate(contextMenuItemPrefab, this.transform);
        contextMenuItem.Text = label;
        contextMenuItem.ContextMenu = this;
        contextMenuItem.SetAction(action);
        return contextMenuItem;
    }

    public void CloseContextMenu()
    {
        if (this != null
            && gameObject != null)
        {
            Destroy(gameObject);
        }
        else
        {
            OpenContextMenus.Remove(this);
        }
    }

    private void OnDestroy()
    {
        // Remove this ContextMenu from the list of opened ContextMenus only after all Input has been released
        // to avoid triggering additional actions (e.g. onClick of button).
        if (CoroutineManager.Instance != null)
        {
            CoroutineManager.Instance.StartCoroutineAlsoForEditor(
                CoroutineUtils.ExecuteWhenConditionIsTrue(
                    () => !InputUtils.AnyKeyboardOrMouseOrTouchPressed(),
                    () => RemoveOpenContextMenuFromList(this)));
        }
        else
        {
            RemoveOpenContextMenuFromList(this);            
        }
    }

    private static void RemoveOpenContextMenuFromList(ContextMenu contextMenu)
    {
        OpenContextMenus.Remove(contextMenu);
    }

    public static void CloseAllOpenContextMenus()
    {
        // Iteration over index because elements are removed during iteration.
        for (int i = 0; i < OpenContextMenus.Count; i++)
        {
            ContextMenu openContextMenu = OpenContextMenus[i];
            openContextMenu.CloseContextMenu();
        }
    }

    public void MoveInsideCanvas()
    {
        // Make sure that all items are visible
        // corners of item in world space
        Vector3[] corners = new Vector3[4];
        RectTransform.GetWorldCorners(corners);

        Vector3[] canvasCorners = new Vector3[4];
        Canvas.GetComponent<RectTransform>().GetWorldCorners(canvasCorners);
        
        float right = corners.Select(it => it.x).Max();
        float canvasRight = canvasCorners.Select(it => it.x).Max();
        if (right > canvasRight)
        {
            float overlapX = right - canvasRight;
            RectTransform.position = new Vector3(RectTransform.position.x - overlapX, RectTransform.position.y, RectTransform.position.z);
        }
        
        float bottom = corners.Select(it => it.y).Min();
        float canvasBottom = canvasCorners.Select(it => it.y).Min();
        if (bottom < canvasBottom)
        {
            float overlapY = canvasBottom - bottom;
            RectTransform.position = new Vector3(RectTransform.position.x, RectTransform.position.y + overlapY, RectTransform.position.z);
        }
    }
}
