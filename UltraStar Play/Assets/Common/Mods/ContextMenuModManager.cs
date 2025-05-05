using System.Collections.Generic;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ContextMenuModManager : AbstractSingletonBehaviour, INeedInjection
{
    public static ContextMenuModManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<ContextMenuModManager>();

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        ContextMenuControl.AnyContextMenuOpenedEventStream
            .Subscribe(contextMenu => OnContextMenuOpened(contextMenu));
    }

    private void OnContextMenuOpened(ContextMenuPopupControl contextMenu)
    {
        List<IContextMenuMod> contextMenuMods = ModManager.GetModObjects<IContextMenuMod>();
        foreach (IContextMenuMod contextMenuMod in contextMenuMods)
        {
            contextMenuMod.FillContextMenu(contextMenu);
        }
    }
}
