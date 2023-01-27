using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

public class ContextMenuPopupManager : AbstractSingletonBehaviour
{
    public static ContextMenuPopupManager Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<ContextMenuPopupManager>("ContextMenuPopupManager");
        }
    }

    [InjectedInInspector]
    public VisualTreeAsset contextMenuUi;

    [InjectedInInspector]
    public VisualTreeAsset contextMenuItemUi;

    [InjectedInInspector]
    public VisualTreeAsset contextMenuSeparatorUi;

    protected override object GetInstance()
    {
        return Instance;
    }
}
