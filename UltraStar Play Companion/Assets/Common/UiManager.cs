using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class UiManager : AbstractSingletonBehaviour, INeedInjection, IBinder
{
    public static UiManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<UiManager>();

    [InjectedInInspector]
    public VisualTreeAsset notificationOverlayUi;

    [InjectedInInspector]
    public VisualTreeAsset notificationUi;

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

    private Label DoCreateNotification(string text)
    {
        VisualElement notificationOverlay = uiDocument.rootVisualElement.Q<VisualElement>("notificationOverlay");
        if (notificationOverlay == null)
        {
            notificationOverlay = notificationOverlayUi.CloneTree().Children().First();
            uiDocument.rootVisualElement.Add(notificationOverlay);
        }

        TemplateContainer templateContainer = notificationUi.CloneTree();
        VisualElement notification = templateContainer.Children().First();
        Label notificationLabel = notification.Q<Label>("notificationLabel");
        notificationLabel.text = text;
        notificationOverlay.Add(notification);

        // Fade out then remove
        StartCoroutine(AnimationUtils.FadeOutThenRemoveVisualElementCoroutine(notification, 2, 1));

        return notificationLabel;
    }

    public static Label CreateNotification(
        string text,
        params string[] additionalTextClasses)
    {
        return Instance.DoCreateNotification(text);
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
