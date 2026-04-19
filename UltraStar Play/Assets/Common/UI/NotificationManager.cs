using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class NotificationManager : AbstractSingletonBehaviour, INeedInjection
{
    public static NotificationManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<NotificationManager>();

    private const float NotificationFadeOutDelayInSeconds = 6;
    private const float NotificationFadeOutDurationInSeconds = 1;

    [InjectedInInspector]
    public VisualTreeAsset notificationUi;

    [InjectedInInspector]
    public VisualTreeAsset notificationOverlayUi;

    [Inject]
    private UIDocument uiDocument;
    
    [Inject]
    private SceneNavigator sceneNavigator;

    private readonly List<Notification> notifications = new();

    private void Start()
    {
        sceneNavigator.SceneChangedEventStream
            .Subscribe(evt => RecreateNotifications())
            .AddTo(gameObject);
    }

    private void Update()
    {
        if (Application.isEditor && Keyboard.current.rightAltKey.wasPressedThisFrame)
        {
            CreateNotification(Translation.Of("Dummy Notification"));
        }
    }
    
    protected override object GetInstance()
    {
        return Instance;
    }

    private void DoCreateNotification(VisualElement contentElement)
    {
        VisualElement notificationElement = notificationUi.CloneTree().Children().First();
        notificationElement.Clear();
        notificationElement.Add(contentElement);

        Notification notification = new Notification(notificationElement, DateTime.UtcNow);
        
        AddNotificationToOverlay(notification);
        AddNotification(notification);
        FadeOutThenRemoveNotification(notification);
    }

    private async void FadeOutThenRemoveNotification(Notification notification)
    {
        await AnimationUtils.FadeOutThenRemoveVisualElementAsync(
                notification.VisualElement,
                NotificationFadeOutDelayInSeconds,
                NotificationFadeOutDurationInSeconds);
        RemoveNotification(notification);
    }

    private VisualElement CreateNotificationOverlay()
    {
        VisualElement notificationOverlay = uiDocument.rootVisualElement.Q<VisualElement>("notificationOverlay");
        if (notificationOverlay == null)
        {
            notificationOverlay = notificationOverlayUi.CloneTree()
                .Children()
                .First();
            uiDocument.rootVisualElement.Add(notificationOverlay);
        }

        return notificationOverlay;
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

    private void AddNotification(Notification notification)
    {
        lock (notifications)
        {
            notifications.Add(notification);
        }
    }

    private async void RemoveNotification(Notification notification)
    {
        await Awaitable.MainThreadAsync();
        notification.VisualElement.RemoveFromHierarchy();
        
        lock (notifications)
        {
            notifications.Remove(notification);
        }
    }

    private void RecreateNotifications()
    {
        lock (notifications)
        {
            foreach (Notification notification in notifications)
            {
                AddNotificationToOverlay(notification);
            }
        }
    }

    private void AddNotificationToOverlay(Notification notification)
    {
        CreateNotificationOverlay().Add(notification.VisualElement);
    }

    private class Notification
    {
        public VisualElement VisualElement { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public Notification(VisualElement visualElement, DateTime createdAt)
        {
            VisualElement = visualElement;
            CreatedAt = createdAt;
        }
    }
}
