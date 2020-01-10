using System;
using System.Collections.Generic;
using UnityEngine;

public class ContextMenu : MonoBehaviour
{
    public ContextMenuItem contextMenuItemPrefab;
    public ContextMenuSeparator contextMenuSeparatorPrefab;

    private RectTransform rectTransform;

    private float lastWidth;
    private float lastHeight;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        // Destroy dummy items from prefab
        transform.DestroyAllDirectChildren();
    }

    void Update()
    {
        UpdatePosition();

        // Close context menu if any mouse button is released.
        // The action of a clicked context menu button will still be triggered.
        for (int mouseButton = 0; mouseButton < 3; mouseButton++)
        {
            if (Input.GetMouseButtonUp(mouseButton))
            {
                Destroy(this.gameObject);
            }
        }
    }

    private void UpdatePosition()
    {
        // Move up and left if out of screen
        if (lastWidth != rectTransform.rect.width)
        {
            lastWidth = rectTransform.rect.width;

            float x = rectTransform.position.x;
            float xOvershoot = (x + rectTransform.rect.width) - Screen.width;
            if (xOvershoot > 0)
            {
                rectTransform.position = new Vector2(x - xOvershoot, rectTransform.position.y);
            }
        }
        if (lastHeight != rectTransform.rect.height)
        {
            lastHeight = rectTransform.rect.height;

            float y = rectTransform.position.y;
            float yOvershoot = (rectTransform.rect.height - y);
            if (yOvershoot > 0)
            {
                rectTransform.position = new Vector2(rectTransform.position.x, y + yOvershoot);
            }
        }
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
        contextMenuItem.SetAction(action);
        return contextMenuItem;
    }
}