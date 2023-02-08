using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        relativePlayerProfileImagePathToAbsolutePath = new();
    }

    public static UiManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<UiManager>();

    private static Dictionary<string, string> relativePlayerProfileImagePathToAbsolutePath = new();

    [InjectedInInspector]
    public VisualTreeAsset notificationOverlayVisualTreeAsset;

    [InjectedInInspector]
    public VisualTreeAsset notificationVisualTreeAsset;

    [InjectedInInspector]
    public VisualTreeAsset dialogUi;

    [InjectedInInspector]
    public VisualTreeAsset accordionUi;

    [InjectedInInspector]
    public Sprite fallbackPlayerProfileImage;

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
        UpdatePlayerProfileImagePaths();
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

    public void UpdatePlayerProfileImagePaths()
    {
        relativePlayerProfileImagePathToAbsolutePath = PlayerProfileUtils.FindPlayerProfileImages();
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
            AccordionItem accordionItem = new(title);
            accordionItem.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            accordionItem.Add(new Label(content));
            helpDialogControl.AddVisualElement(accordionItem);
        }

        titleToContentMap.ForEach(entry => AddChapter(entry.Key, entry.Value));

        Button closeDialogButton = helpDialogControl.AddButton(TranslationManager.GetTranslation(R.Messages.close),
            onCloseHelp);
        closeDialogButton.Focus();

        return helpDialogControl;
    }

    public void LoadPlayerProfileImage(string imagePath, Action<Sprite> onSuccess)
    {
        if (imagePath.IsNullOrEmpty())
        {
            onSuccess(fallbackPlayerProfileImage);
            return;
        }

        string relativePathNormalized = PathUtils.NormalizePath(imagePath);
        string matchingFullPath = GetAbsolutePlayerProfileImagePaths().FirstOrDefault(absolutePath =>
        {
            string absolutePathNormalized = PathUtils.NormalizePath(absolutePath);
            return absolutePathNormalized.EndsWith(relativePathNormalized);
        });
        if (matchingFullPath.IsNullOrEmpty())
        {
            Debug.LogWarning($"Cannot load player profile image with path '{imagePath}' (normalized: '{relativePathNormalized}'), no corresponding image file found.");
            onSuccess(fallbackPlayerProfileImage);
            return;
        }

        ImageManager.LoadSpriteFromUri(matchingFullPath, onSuccess);
    }

    public List<string> GetAbsolutePlayerProfileImagePaths()
    {
        return relativePlayerProfileImagePathToAbsolutePath.Values.ToList();
    }

    public List<string> GetRelativePlayerProfileImagePaths(bool includeWebCamImages)
    {
        if (includeWebCamImages)
        {
            return relativePlayerProfileImagePathToAbsolutePath.Keys.ToList();
        }
        else
        {
            return relativePlayerProfileImagePathToAbsolutePath.Keys
                .Where(relativePath => !relativePath.Contains(PlayerProfileUtils.PlayerProfileWebCamImagesFolderName))
                .ToList();
        }

    }
}
