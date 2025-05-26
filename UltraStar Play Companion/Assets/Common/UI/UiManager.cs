using System.Collections.Generic;
using UniInject;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class UiManager : AbstractSingletonBehaviour, INeedInjection, IBinder
{
    public static UiManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<UiManager>();

    [InjectedInInspector]
    public VisualTreeAsset messageDialogUi;

    [InjectedInInspector]
    public VisualTreeAsset micWithNameUi;

    [InjectedInInspector]
    public VisualTreeAsset songListEntryUi;

    [InjectedInInspector]
    public VisualTreeAsset songQueueEntryUi;

    [InjectedInInspector]
    public VisualTreeAsset songQueuePlayerEntryUi;

    [Inject]
    private UIDocument uiDocument;

    protected override object GetInstance()
    {
        return Instance;
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new();
        bb.Bind(nameof(messageDialogUi)).ToExistingInstance(messageDialogUi);
        bb.Bind(nameof(micWithNameUi)).ToExistingInstance(micWithNameUi);
        bb.Bind(nameof(songListEntryUi)).ToExistingInstance(songListEntryUi);
        bb.Bind(nameof(songQueueEntryUi)).ToExistingInstance(songQueueEntryUi);
        bb.Bind(nameof(songQueuePlayerEntryUi)).ToExistingInstance(songQueuePlayerEntryUi);
        return bb.GetBindings();
    }
}
