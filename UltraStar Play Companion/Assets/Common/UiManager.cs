using System.Linq;
using UniInject;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class UiManager : AbstractSingletonBehaviour, INeedInjection
{
    public static UiManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<UiManager>();

    [InjectedInInspector]
    public VisualTreeAsset notificationOverlayUi;

    [InjectedInInspector]
    public VisualTreeAsset notificationUi;

    [Inject]
    private UIDocument uiDocument;
    
    protected override object GetInstance()
    {
        return Instance;
    }

    private Label DoCreateNotification(
        string text)
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
        string text)
    {
        return Instance.DoCreateNotification(text);
    }
}
