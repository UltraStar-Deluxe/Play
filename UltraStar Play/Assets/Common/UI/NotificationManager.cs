using System.Linq;
using UniInject;
using UnityEngine.UIElements;

public class NotificationManager : AbstractSingletonBehaviour, INeedInjection
{
    public static NotificationManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<NotificationManager>();

    private const float NotificationFadeOutDelayInSeconds = 4;
    private const float NotificationFadeOutDurationInSeconds = 1;

    [InjectedInInspector]
    public VisualTreeAsset notificationUi;

    [InjectedInInspector]
    public VisualTreeAsset notificationOverlayUi;

    [Inject]
    private UIDocument uiDocument;

    protected override object GetInstance()
    {
        return Instance;
    }

    private Label DoCreateNotification(Translation text)
    {
        VisualElement notificationOverlay = uiDocument.rootVisualElement.Q<VisualElement>("notificationOverlay");
        if (notificationOverlay == null)
        {
            notificationOverlay = notificationOverlayUi.CloneTree()
                .Children()
                .First();
            uiDocument.rootVisualElement.Add(notificationOverlay);
        }

        TemplateContainer templateContainer = notificationUi.CloneTree();
        VisualElement notification = templateContainer.Children().First();
        Label notificationLabel = notification.Q<Label>("notificationLabel");
        notificationLabel.SetTranslatedText(text);
        notificationOverlay.Add(notification);

        // Fade out then remove
        StartCoroutine(AnimationUtils.FadeOutThenRemoveVisualElementCoroutine(notification, NotificationFadeOutDelayInSeconds, NotificationFadeOutDurationInSeconds));

        return notificationLabel;
    }

    public static void CreateNotification(Translation text)
    {
        ThreadUtils.RunOnMainThread(() =>
        {
            NotificationManager notificationManager = Instance;
            if (notificationManager == null)
            {
                return;
            }

            notificationManager.DoCreateNotification(text);
        });
    }
}
