using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

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

    private readonly List<RectTransform> debugPoints = new List<RectTransform>();

    [InjectedInInspector]
    public VisualTreeAsset notificationOverlayVisualTreeAsset;

    [InjectedInInspector]
    public VisualTreeAsset notificationVisualTreeAsset;

    [InjectedInInspector]
    public RectTransform debugPositionIndicatorPrefab;

    [InjectedInInspector]
    public ContextMenu contextMenuPrefab;

    [InjectedInInspector]
    public VisualTreeAsset contextMenuUi;

    [InjectedInInspector]
    public VisualTreeAsset contextMenuItemUi;

    [InjectedInInspector]
    public VisualTreeAsset contextMenuSeparatorUi;

    [InjectedInInspector]
    public ShowFps showFpsPrefab;

    private Canvas canvas;
    private RectTransform canvasRectTransform;

    [Inject]
    private Injector injector;

    [Inject(Optional = true)]
    private UIDocument uiDocument;

    private ShowFps showFpsInstance;

    private void Awake()
    {
        LeanTween.init(800);
    }

    private void Start()
    {
        if (SettingsManager.Instance.Settings.DeveloperSettings.showFps)
        {
            CreateShowFpsInstance();
        }
    }

    private void Update()
    {
        ContextMenuPopupControl.OpenContextMenuPopups
            .ForEach(contextMenuPopupControl => contextMenuPopupControl.Update());
    }

    public void CreateShowFpsInstance()
    {
        if (showFpsInstance != null)
        {
            return;
        }

        showFpsInstance = Instantiate(showFpsPrefab);
        injector.Inject(showFpsInstance);
        // Move to front
        showFpsInstance.transform.SetAsLastSibling();
        showFpsInstance.transform.position = new Vector3(20, 20, 0);
    }

    public void DestroyShowFpsInstance()
    {
        if (showFpsInstance != null)
        {
            Destroy(showFpsInstance);
        }
    }

    public void DestroyAllDebugPoints()
    {
        foreach (RectTransform debugPoint in debugPoints)
        {
            GameObject.Destroy(debugPoint.gameObject);
        }
        debugPoints.Clear();
    }

    public Label CreateNotificationVisualElement(
        string text,
        params string[] additionalTextClasses)
    {
        if (uiDocument == null)
        {
            return null;
        }

        VisualElement notificationOverlay = uiDocument.rootVisualElement.Q<VisualElement>(R.UxmlNames.notificationOverlay);
        if (notificationOverlay == null)
        {
            notificationOverlay = notificationOverlayVisualTreeAsset.CloneTree()
                .Children()
                .First();
            uiDocument.rootVisualElement.Add(notificationOverlay);
        }

        TemplateContainer templateContainer = notificationVisualTreeAsset.CloneTree();
        VisualElement notification = templateContainer.Children().First();
        Label notificationLabel = notification.Q<Label>(R.UxmlNames.notificationLabel);
        notificationLabel.text = text;
        if (additionalTextClasses != null)
        {
            additionalTextClasses.ForEach(className => notificationLabel.AddToClassList(className));
        }
        notificationOverlay.Add(notification);

        // Fade out then remove
        StartCoroutine(FadeOutVisualElement(notification, 2, 1));

        return notificationLabel;
    }

    public static IEnumerator FadeOutVisualElement(
        VisualElement visualElement,
        float solidTimeInSeconds,
        float fadeOutTimeInSeconds)
    {
        yield return new WaitForSeconds(solidTimeInSeconds);
        float startOpacity = visualElement.resolvedStyle.opacity;
        float startTime = Time.time;
        while (visualElement.resolvedStyle.opacity > 0)
        {
            float newOpacity = Mathf.Lerp(startOpacity, 0, (Time.time - startTime) / fadeOutTimeInSeconds);
            if (newOpacity < 0)
            {
                newOpacity = 0;
            }

            visualElement.style.opacity = newOpacity;
            yield return null;
        }

        // Remove VisualElement
        if (visualElement.parent != null)
        {
            visualElement.parent.Remove(visualElement);
        }
    }
}
