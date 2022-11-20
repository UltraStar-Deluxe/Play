using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProTrans;
using UniInject;
using UnityEngine;
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

    [InjectedInInspector]
    public VisualTreeAsset notificationOverlayVisualTreeAsset;

    [InjectedInInspector]
    public VisualTreeAsset notificationVisualTreeAsset;

    [InjectedInInspector]
    public VisualTreeAsset dialogUi;

    [InjectedInInspector]
    public VisualTreeAsset accordionUi;

    [InjectedInInspector]
    public ShowFps showFpsPrefab;

    [InjectedInInspector]
    public List<AvatarImageReference> avatarImageReferences;

    [Inject]
    private Injector injector;

    [Inject(Optional = true)]
    private UIDocument uiDocument;

    private ShowFps showFpsInstance;

    private void Awake()
    {
        LeanTween.init(10000);
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

    public Label CreateNotificationVisualElement(
        string text,
        params string[] additionalTextClasses)
    {
        if (uiDocument == null)
        {
            return null;
        }

        VisualElement notificationOverlay = uiDocument.rootVisualElement.Q<VisualElement>("notificationOverlay");
        if (notificationOverlay == null)
        {
            notificationOverlay = notificationOverlayVisualTreeAsset.CloneTree()
                .Children()
                .First();
            uiDocument.rootVisualElement.Children().First().Add(notificationOverlay);
        }

        TemplateContainer templateContainer = notificationVisualTreeAsset.CloneTree();
        VisualElement notification = templateContainer.Children().First();
        Label notificationLabel = notification.Q<Label>("notificationLabel");
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

    public Sprite GetAvatarSprite(EAvatar avatar)
    {
        AvatarImageReference avatarImageReference = avatarImageReferences
            .FirstOrDefault(it => it.avatar == avatar);
        return avatarImageReference?.sprite;
    }

    public MessageDialogControl CreateHelpDialogControl(Dictionary<string, string> titleToContentMap, Action onCloseHelp)
    {
        VisualElement helpDialog = dialogUi.CloneTree().Children().FirstOrDefault();
        uiDocument.rootVisualElement.Add(helpDialog);
        helpDialog.AddToClassList("wordWrap");

        MessageDialogControl helpDialogControl = injector
            .WithRootVisualElement(helpDialog)
            .CreateAndInject<MessageDialogControl>();
        helpDialogControl.Title = TranslationManager.GetTranslation(R.Messages.options_recording_helpDialog_title);

        void AddChapter(string title, string content)
        {
            AccordionItemControl accordionItemControl = CreateAccordionItemControl();
            accordionItemControl.Title = title;
            accordionItemControl.AddVisualElement(new Label(content));
            helpDialogControl.AddVisualElement(accordionItemControl.VisualElement);
        }

        titleToContentMap.ForEach(entry => AddChapter(entry.Key, entry.Value));

        Button closeDialogButton = helpDialogControl.AddButton(TranslationManager.GetTranslation(R.Messages.close),
            onCloseHelp);
        closeDialogButton.Focus();

        return helpDialogControl;
    }

    public AccordionItemControl CreateAccordionItemControl()
    {
        VisualElement accordionItem = accordionUi.CloneTree().Children().FirstOrDefault();
        AccordionItemControl accordionItemControl = injector
            .WithRootVisualElement(accordionItem)
            .CreateAndInject<AccordionItemControl>();
        return accordionItemControl;
    }
}
