using System.Linq;
using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

public class ContextMenuPopupManager : AbstractSingletonBehaviour, ISceneInjectionFinishedListener
{
    public static ContextMenuPopupManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<ContextMenuPopupManager>();

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

    public void OnSceneInjectionFinished()
    {
        // Close context menu via "back" InputAction with high priority
        InputManager.GetInputAction("usplay/back").PerformedAsObservable(100)
            .Subscribe(context =>
            {
                if (ContextMenuPopupControl.OpenContextMenuPopups.IsNullOrEmpty())
                {
                    return;
                }
                ContextMenuPopupControl.OpenContextMenuPopups.FirstOrDefault().CloseContextMenu();
                InputManager.GetInputAction("usplay/back").CancelNotifyForThisFrame();
            })
            .AddTo(gameObject);
    }
}
