using System;
using System.Collections.Generic;
using System.Linq;
using ProTrans;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class UiManager : AbstractSingletonBehaviour, INeedInjection, IBinder
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        relativePlayerProfileImagePathToAbsolutePath = new();
    }

    public static UiManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<UiManager>();

    private static Dictionary<string, string> relativePlayerProfileImagePathToAbsolutePath = new();

    [InjectedInInspector]
    public VisualTreeAsset notificationOverlayUi;

    [InjectedInInspector]
    public VisualTreeAsset notificationUi;

    [InjectedInInspector]
    public VisualTreeAsset messageDialogUi;

    [InjectedInInspector]
    public VisualTreeAsset micWithNameUi;
    
    [InjectedInInspector]
    public Sprite fallbackPlayerProfileImage;

    [InjectedInInspector]
    public VisualTreeAsset nextGameRoundInfoUi;

    [InjectedInInspector]
    public VisualTreeAsset nextGameRoundInfoPlayerEntryUi;

    [InjectedInInspector]
    public VisualTreeAsset songQueueEntryUi;
    
    [InjectedInInspector]
    public VisualTreeAsset songQueuePlayerEntryUi;
    
    [InjectedInInspector]
    public Sprite defaultSongImage;
    
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
        string text)
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
        notificationLabel.text = text;
        notificationOverlay.Add(notification);

        // Fade out then remove
        StartCoroutine(AnimationUtils.FadeOutThenRemoveVisualElementCoroutine(notification, 2, 1));

        return notificationLabel;
    }

    public void UpdatePlayerProfileImagePaths()
    {
        relativePlayerProfileImagePathToAbsolutePath = PlayerProfileUtils.FindPlayerProfileImages();
    }

    public static Label CreateNotification(
        string text)
    {
        return Instance.DoCreateNotification(text);
    }

    public MessageDialogControl CreateDialogControl(string dialogTitle)
    {
        VisualElement dialogVisualElement = messageDialogUi.CloneTree().Children().FirstOrDefault();
        uiDocument.rootVisualElement.Add(dialogVisualElement);
        dialogVisualElement.AddToClassList("wordWrap");

        MessageDialogControl dialogControl = injector
            .WithRootVisualElement(dialogVisualElement)
            .CreateAndInject<MessageDialogControl>();
        dialogControl.Title = dialogTitle;

        return dialogControl;
    }
    
    public MessageDialogControl CreateHelpDialogControl(
        string dialogTitle,
        Dictionary<string, string> titleToContentMap)
    {
        VisualElement dialogVisualElement = messageDialogUi.CloneTree().Children().FirstOrDefault();
        uiDocument.rootVisualElement.Add(dialogVisualElement);
        dialogVisualElement.AddToClassList("wordWrap");
        
        MessageDialogControl dialogControl = injector
            .WithRootVisualElement(dialogVisualElement)
            .CreateAndInject<MessageDialogControl>();
        dialogControl.Title = dialogTitle;

        AccordionGroup accordionGroup = new();
        dialogControl.AddVisualElement(accordionGroup);
            
        void AddChapter(string title, string content)
        {
            AccordionItem accordionItem = new(title);
            accordionItem.Add(new Label(content));
            accordionGroup.Add(accordionItem);
        }

        titleToContentMap.ForEach(entry => AddChapter(entry.Key, entry.Value));

        return dialogControl;
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
    
    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new();
        bb.Bind(nameof(messageDialogUi)).ToExistingInstance(messageDialogUi);
        bb.Bind(nameof(nextGameRoundInfoUi)).ToExistingInstance(nextGameRoundInfoUi);
        bb.Bind(nameof(nextGameRoundInfoPlayerEntryUi)).ToExistingInstance(nextGameRoundInfoPlayerEntryUi);
        bb.Bind(nameof(micWithNameUi)).ToExistingInstance(micWithNameUi);
        bb.Bind(nameof(songQueueEntryUi)).ToExistingInstance(songQueueEntryUi);
        bb.Bind(nameof(songQueuePlayerEntryUi)).ToExistingInstance(songQueuePlayerEntryUi);
        return bb.GetBindings();
    }
}
