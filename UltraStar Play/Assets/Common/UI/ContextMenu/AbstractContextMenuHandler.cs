using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UnityEngine.InputSystem;

public abstract class AbstractContextMenuHandler : MonoBehaviour
{
    private Canvas canvas;
    private Canvas Canvas
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

    private RectTransform rectTransform;
    private RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            return rectTransform;
        }
    }
    
    protected abstract void FillContextMenu(ContextMenu contextMenu);

    private List<IDisposable> disposables = new List<IDisposable>();
    
    protected void Start()
    {
        disposables.Add(InputManager.GetInputAction(R.InputActions.usplay_openContextMenu).PerformedAsObservable()
            .Where(context => context.ReadValueAsButton())
            .Subscribe(CheckOpenContextMenuFromInputAction));
    }

    private void CheckOpenContextMenuFromInputAction(InputAction.CallbackContext context)
    {
        if (Pointer.current == null
            || !context.ReadValueAsButton())
        {
            return;
        }

        Vector2 position = new Vector2(Pointer.current.position.x.ReadValue(), Pointer.current.position.y.ReadValue());
        if (!RectTransformUtility.RectangleContainsScreenPoint(RectTransform, position))
        {
            return;
        }

        ContextMenu contextMenu = OpenContextMenu();
        contextMenu.RectTransform.position = position;
    }

    public ContextMenu OpenContextMenu()
    {
        ContextMenu.CloseAllOpenContextMenus();
        
        ContextMenu contextMenuPrefab = GetContextMenuPrefab();
        ContextMenu contextMenu = Instantiate(contextMenuPrefab, Canvas.transform);
        FillContextMenu(contextMenu);
        return contextMenu;
    }
    
    private ContextMenu GetContextMenuPrefab()
    {
        return UiManager.Instance.contextMenuPrefab;
    }

    private void OnDestroy()
    {
        disposables.ForEach(it => it.Dispose());
    }
}
