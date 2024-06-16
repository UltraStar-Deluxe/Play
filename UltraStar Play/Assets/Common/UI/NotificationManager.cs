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

    private void DoCreateNotification(VisualElement content)
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
        notification.Clear();
        notification.Add(content);
        notificationOverlay.Add(notification);

        // Fade out then remove
        StartCoroutine(AnimationUtils.FadeOutThenRemoveVisualElementCoroutine(notification, NotificationFadeOutDelayInSeconds, NotificationFadeOutDurationInSeconds));
    }

    public static void CreateNotification(VisualElement content)
    {
        ThreadUtils.RunOnMainThread(() =>
        {
            NotificationManager notificationManager = Instance;
            if (notificationManager == null)
            {
                return;
            }

            notificationManager.DoCreateNotification(content);
        });
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

            Label label = new Label();
            label.name = "notificationLabel";
            label.SetTranslatedText(text);
            notificationManager.DoCreateNotification(label);
        });
    }
}
