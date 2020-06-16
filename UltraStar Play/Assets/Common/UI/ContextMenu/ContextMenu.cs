using System;
using UnityEngine;

public class ContextMenu : AbstractPointerSensitivePopup
{
    public ContextMenuItem contextMenuItemPrefab;
    public ContextMenuSeparator contextMenuSeparatorPrefab;

    protected override void Awake()
    {
        base.Awake();

        // Destroy dummy items from prefab
        transform.DestroyAllDirectChildren();
    }

    protected override void Update()
    {
        base.Update();

        // Close popup if any mouse button is released.
        // The action of a clicked context menu button will still be triggered.
        for (int mouseButton = 0; mouseButton < 3; mouseButton++)
        {
            if (Input.GetMouseButtonUp(mouseButton))
            {
                Destroy(this.gameObject);
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
