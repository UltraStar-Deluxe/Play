using System;
using System.Collections.Generic;
using System.Linq;
using BsiGame.UI.UIElements;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class UiManager : AbstractSingletonBehaviour, INeedInjection, IBinder, IInjectionFinishedListener
{
    public static UiManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<UiManager>();

    private readonly Subject<ChildrenChangedEvent> childrenChangedEventStream = new();
    public IObservable<ChildrenChangedEvent> ChildrenChangedEventStream => childrenChangedEventStream;

    private int lastScreenWidth;
    private int lastScreenHeight;
    private readonly Subject<ScreenSizeChangedEvent> screenSizeChangedEventStream = new();
    public IObservable<ScreenSizeChangedEvent> ScreenSizeChangedEventStream => screenSizeChangedEventStream;

    [InjectedInInspector]
    public VisualTreeAsset messageDialogUi;

    [InjectedInInspector]
    public VisualTreeAsset micWithNameUi;

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

    [InjectedInInspector]
    public Sprite defaultFolderImage;

    [Inject]
    private Injector injector;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private Settings settings;

    private readonly HashSet<VisualElement> visualElementsWithChildChangeManipulator = new();

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void AwakeSingleton()
    {
        LeanTween.init(10000);
    }

    protected override void StartSingleton()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
    }

    public void OnInjectionFinished()
    {
        // The UIDocument can change when the scene changes. Thus, registering events must be done in OnInjectionFinished.
        RegisterChildrenChangedEvent();
    }

    private void RegisterChildrenChangedEvent()
    {
        if (visualElementsWithChildChangeManipulator.Contains(uiDocument.rootVisualElement))
        {
            return;
        }
        visualElementsWithChildChangeManipulator.Add(uiDocument.rootVisualElement);
        uiDocument.rootVisualElement.AddManipulator(new ChildChangeManipulator());
        uiDocument.rootVisualElement.RegisterCallback<ChildChangeEvent>(evt => childrenChangedEventStream.OnNext(new ChildrenChangedEvent()
        {
            targetParent = evt.targetParent,
            targetChild = evt.targetChild,
            newChildCount = evt.newChildCount,
            previousChildCount = evt.previousChildCount,
        }));
    }

    private void Update()
    {
        ContextMenuPopupControl.OpenContextMenuPopups
            .ForEach(contextMenuPopupControl => contextMenuPopupControl.Update());

        UpdateScreenSize();
    }

    private void UpdateScreenSize()
    {
        if (lastScreenHeight != Screen.height
            || lastScreenWidth != Screen.width)
        {
            screenSizeChangedEventStream.OnNext(new ScreenSizeChangedEvent(
                new Vector2Int(lastScreenWidth, lastScreenHeight),
                new Vector2Int(Screen.width, Screen.height)));
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
        }
    }

    public MessageDialogControl CreateDialogControl(Translation dialogTitle)
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

    public MessageDialogControl CreateErrorInfoDialogControl(
        Translation dialogTitle,
        Translation dialogMessage,
        Translation errorMessage,
        Translation closeButtonText = default)
    {
        MessageDialogControl messageDialogControl = CreateInfoDialogControl(dialogTitle, dialogMessage, closeButtonText);

        // Add accordion item to show error message.
        if (errorMessage.Value.IsNullOrEmpty())
        {
            return messageDialogControl;
        }

        AccordionItem accordionItem = new AccordionItem();
        accordionItem.SetTranslatedTitle(Translation.Get(R.Messages.common_details));
        Label errorMessageLabel = new();
        errorMessageLabel.SetTranslatedText(errorMessage);
        accordionItem.Add(errorMessageLabel);
        accordionItem.HideAccordionContent();
        messageDialogControl.AddVisualElement(accordionItem);

        return messageDialogControl;
    }

    public MessageDialogControl CreateInfoDialogControl(
        Translation dialogTitle,
        Translation dialogMessage,
        Translation closeButtonText = default)
    {
        MessageDialogControl messageDialogControl = CreateDialogControl(dialogTitle);
        messageDialogControl.Message = dialogMessage;

        closeButtonText = !closeButtonText.Value.IsNullOrEmpty()
            ? closeButtonText
            : Translation.Get(R.Messages.action_close);
        messageDialogControl.AddButton(closeButtonText, evt =>
        {
            messageDialogControl.CloseDialog();
        });
        return messageDialogControl;
    }

    public MessageDialogControl CreateConfirmationDialogControl(
        Translation dialogTitle,
        Translation dialogMessage,
        Translation confirmButtonText,
        Action<EventBase> onConfirm,
        Translation cancelButtonText = default,
        Action<EventBase> onCancel = null)
    {
        MessageDialogControl messageDialogControl = CreateDialogControl(dialogTitle);
        messageDialogControl.Message = dialogMessage;
        messageDialogControl.AddButton(confirmButtonText, evt =>
        {
            messageDialogControl.CloseDialog();
            onConfirm?.Invoke(evt);
        });

        cancelButtonText = !cancelButtonText.Value.IsNullOrEmpty()
            ? cancelButtonText
            : Translation.Get(R.Messages.action_cancel);
        messageDialogControl.AddButton(cancelButtonText, evt =>
        {
            messageDialogControl.CloseDialog();
            onCancel?.Invoke(evt);
        });
        return messageDialogControl;
    }

    public MessageDialogControl CreateHelpDialogControl(
        Translation dialogTitle,
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
