using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class UiManager : MonoBehaviour, INeedInjection
{
    public static UiManager Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<UiManager>("UiManager");
        }
    }

    public WarningDialog warningDialogPrefab;
    public Notification notificationPrefab;

    private Canvas canvas;
    private RectTransform canvasRectTransform;
    private float notificationHeightInPixels;
    private float notificationWidthInPixels;

    [Inject]
    private Injector injector;

    private readonly List<Notification> notifications = new List<Notification>();

    void Start()
    {
        notificationHeightInPixels = notificationPrefab.GetComponent<RectTransform>().rect.height;
        notificationWidthInPixels = notificationPrefab.GetComponent<RectTransform>().rect.width;
    }

    public WarningDialog CreateWarningDialog(string title, string message)
    {
        FindCanvas();

        WarningDialog warningDialog = Instantiate(warningDialogPrefab, canvas.transform);
        injector.Inject(warningDialog);
        warningDialog.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        if (title != null)
        {
            warningDialog.Title = title;
        }
        if (message != null)
        {
            warningDialog.Message = message;
        }
        return warningDialog;
    }

    public Notification CreateNotification(string message)
    {
        FindCanvas();

        Notification notification = Instantiate(notificationPrefab, canvas.transform);
        injector.Inject(notification);
        notification.SetText(message);
        PositionNotification(notification, notifications.Count);

        notifications.Add(notification);
        return notification;
    }

    private void FindCanvas()
    {
        if (canvas == null)
        {
            canvas = CanvasUtils.FindCanvas();
            canvasRectTransform = canvas.GetComponent<RectTransform>();
        }
    }

    private void PositionNotification(Notification notification, int index)
    {
        float anchoredHeight = notificationHeightInPixels / canvasRectTransform.rect.height;
        float anchoredWidth = notificationWidthInPixels / canvasRectTransform.rect.width;
        float x = 0;
        float y = (index * notificationHeightInPixels) / canvasRectTransform.rect.height;
        notification.RectTransform.anchorMin = new Vector2(x, y);
        notification.RectTransform.anchorMax = new Vector2(x + anchoredWidth, y + anchoredHeight);
        notification.RectTransform.sizeDelta = Vector2.zero;
        notification.RectTransform.anchoredPosition = Vector2.zero;
    }

    public void OnNotificationDestroyed(Notification notification)
    {
        notifications.Remove(notification);
        int index = 0;
        foreach (Notification n in notifications)
        {
            PositionNotification(n, index);
            index++;
        }
    }
}
