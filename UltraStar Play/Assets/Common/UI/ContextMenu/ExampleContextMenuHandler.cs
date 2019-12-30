using UnityEngine;

public class ExampleContextMenuHandler : AbstractContextMenuHandler
{
    protected override void FillContextMenu(ContextMenu contextMenu)
    {
        contextMenu.AddItem("Item 1", () => Debug.Log("Item 1 clicked"));
        contextMenu.AddSeparator();
        contextMenu.AddItem("Item 2", () => Debug.Log("Item 2 clicked"));
    }
}
