using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UniRx;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public abstract class AbstractContextMenuHandler : MonoBehaviour, INeedInjection, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    public const float DragDistanceThreshold = 10f;
    
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

    [Inject(optional = true)]
    protected GraphicRaycaster graphicRaycaster;
    
    [Inject(optional = true)]
    protected EventSystem eventSystem;
    
    protected abstract void FillContextMenu(ContextMenu contextMenu);

    public bool IsDrag { get; private set; }
    private Vector2 dragStartPosition;
    
    protected void Start()
    {
        InputManager.GetInputAction(R.InputActions.usplay_openContextMenu).PerformedAsObservable()
            .Subscribe(CheckOpenContextMenuFromInputAction)
            .AddTo(gameObject);
    }

    protected virtual void CheckOpenContextMenuFromInputAction(InputAction.CallbackContext context)
    {
        if (Pointer.current == null
            || !context.ReadValueAsButton()
            || IsDrag
            || Touch.activeTouches.Count >= 2)
        {
            return;
        }

        Vector2 position = new Vector2(Pointer.current.position.x.ReadValue(), Pointer.current.position.y.ReadValue());
        if (!RectTransformUtility.RectangleContainsScreenPoint(RectTransform, position))
        {
            return;
        }

        if (graphicRaycaster != null && eventSystem != null)
        {
            List<RaycastResult> raycastResults = new List<RaycastResult>();
            PointerEventData pointerEventData = new PointerEventData(eventSystem);
            pointerEventData.position = position;
            graphicRaycaster.Raycast(pointerEventData, raycastResults);
            if (raycastResults.FirstOrDefault().gameObject != this.gameObject)
            {
                // ContextMenu opened on some other element
                return;
            }
        }
        
        OpenContextMenu(position);
    }

    public void OpenContextMenu(Vector2 position)
    {
        ContextMenu.CloseAllOpenContextMenus();
        
        ContextMenu contextMenuPrefab = GetContextMenuPrefab();
        ContextMenu contextMenu = Instantiate(contextMenuPrefab, Canvas.transform);
        contextMenu.RectTransform.position = position;
        FillContextMenu(contextMenu);
    }
    
    private ContextMenu GetContextMenuPrefab()
    {
        return UiManager.Instance.contextMenuPrefab;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragStartPosition = eventData.position;
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (Vector2.Distance(eventData.position, dragStartPosition) > DragDistanceThreshold)
        {
            IsDrag = true;
        }
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        IsDrag = false;
    }
}
