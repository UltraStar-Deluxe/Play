using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class NotificationManager : AbstractSingletonBehaviour, INeedInjection
{
    public static NotificationManager Instance
    {
        get => DontDestroyOnLoadManager.FindComponentOrThrow<NotificationManager>();
    }

    private const float NotificationFadeOutDelayInSeconds = 4;
    private const float NotificationFadeOutDurationInSeconds = 1;

    [InjectedInInspector] public VisualTreeAsset notificationUi;

    [InjectedInInspector] public VisualTreeAsset notificationOverlayUi;

    [Inject] private UIDocument uiDocument;

    protected override object GetInstance()
    {
        return Instance;
    }

    private async Awaitable DoCreateNotification(VisualElement content)
    {
        await Awaitable.MainThreadAsync();

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

        // Fade out then remove, without await to run concurrently
        AnimationUtils.FadeOutThenRemoveVisualElementAsync(notification, NotificationFadeOutDelayInSeconds, NotificationFadeOutDurationInSeconds);
    }

    public static void CreateNotification(VisualElement content)
    {
        Instance?.DoCreateNotification(content);
    }

    public static void CreateNotification(Translation text)
    {
        Label label = new();
        label.name = "notificationLabel";
        label.SetTranslatedText(text);
        CreateNotification(label);
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (Keyboard.current.rightCtrlKey.wasPressedThisFrame)
        {
            CreateNotification(Translation.Of("Ctrl pressed"));
        }
    }
#endif
}
