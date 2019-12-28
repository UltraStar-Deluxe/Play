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

    protected abstract void FillContextMenu(ContextMenu contextMenu);

    public void OnPointerClick(PointerEventData ped)
    {
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