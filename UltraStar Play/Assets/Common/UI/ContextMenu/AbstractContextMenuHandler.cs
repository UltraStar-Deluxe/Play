using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

abstract public class AbstractContextMenuHandler : MonoBehaviour, IPointerClickHandler
{
    // The fields of this class are loaded lazy instead of in Awake,
    // because if the context menu would never be opened then the fields would never be used.
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

    private GraphicRaycaster graphicRaycaster;
    private GraphicRaycaster GraphicRaycaster
    {
        get
        {
            if (graphicRaycaster == null)
            {
                graphicRaycaster = Canvas.GetComponent<GraphicRaycaster>();
            }
            return graphicRaycaster;
        }
    }

    protected abstract void FillContextMenu(ContextMenu contextMenu);

    public void OnPointerClick(PointerEventData ped)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(RectTransform,
                                                                     ped.position,
                                                                     ped.pressEventCamera,
                                                                     out Vector2 localPoint))
        {
            return;
        }

        // Check that this was clicked, and not another element behind or inside of it.
        List<RaycastResult> results = new List<RaycastResult>();
        GraphicRaycaster.Raycast(ped, results);
        if (results.Count == 0 || results[0].gameObject != gameObject)
        {
            return;
        }

        if (ped.button == PointerEventData.InputButton.Right)
        {
            ContextMenu contextMenu = OpenContextMenu();
            RectTransform contextMenuRectTransform = contextMenu.GetComponent<RectTransform>();
            contextMenuRectTransform.position = ped.position;
        }
    }

    public ContextMenu OpenContextMenu()
    {
        ContextMenu contextMenuPrefab = GetContextMenuPrefab();
        ContextMenu contextMenu = Instantiate(contextMenuPrefab, Canvas.transform);
        FillContextMenu(contextMenu);
        return contextMenu;
    }

    private ContextMenu GetContextMenuPrefab()
    {
        return ContextMenuManager.Instance.contextMenuPrefab;
    }
}