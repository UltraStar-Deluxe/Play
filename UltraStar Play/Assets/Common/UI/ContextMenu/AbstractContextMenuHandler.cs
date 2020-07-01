using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

abstract public class AbstractContextMenuHandler : MonoBehaviour, IPointerClickHandler
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

    protected abstract void FillContextMenu(ContextMenu contextMenu);

    public void OnPointerClick(PointerEventData ped)
    {
        if (ped.button == PointerEventData.InputButton.Right)
        {
            ContextMenu contextMenu = OpenContextMenu();
            contextMenu.RectTransform.position = ped.position;
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
        return UiManager.Instance.contextMenuPrefab;
    }
}
