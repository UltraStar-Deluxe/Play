using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class UiManager : AbstractSingletonBehaviour, INeedInjection
{
    public static UiManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<UiManager>();

    [InjectedInInspector]
    public VisualTreeAsset notificationOverlayVisualTreeAsset;

    [InjectedInInspector]
    public VisualTreeAsset notificationVisualTreeAsset;

    [InjectedInInspector]
    public VisualTreeAsset dialogUi;

    [InjectedInInspector]
    public VisualTreeAsset accordionUi;

    [InjectedInInspector]
    public List<AvatarImageReference> avatarImageReferences;

    [Inject]
    private Injector injector;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private Settings settings;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void AwakeSingleton()
    {
        LeanTween.init(10000);
    }

    private void Update()
    {
        ContextMenuPopupControl.OpenContextMenuPopups
            .ForEach(contextMenuPopupControl => contextMenuPopupControl.Update());
    }

    private Label DoCreateNotification(
        string text,
        params string[] additionalTextClasses)
    {
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

    public static Label CreateNotification(
        string text,
        params string[] additionalTextClasses)
    {
        return Instance.DoCreateNotification(text, additionalTextClasses);
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

    public MessageDialogControl CreateHelpDialogControl(string dialogTitle, Dictionary<string, string> titleToContentMap, Action onCloseHelp)
    {
        VisualElement helpDialog = dialogUi.CloneTree().Children().FirstOrDefault();
        uiDocument.rootVisualElement.Add(helpDialog);
        helpDialog.AddToClassList("wordWrap");

        MessageDialogControl helpDialogControl = injector
            .WithRootVisualElement(helpDialog)
            .CreateAndInject<MessageDialogControl>();
        helpDialogControl.Title = dialogTitle;

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
