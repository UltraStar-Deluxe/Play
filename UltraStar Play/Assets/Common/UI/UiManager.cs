using System;
using System.Collections.Generic;
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
      
        // TODO: There is a crash when using ChildChangeManipulator with IL2CPP.
#if !ENABLE_IL2CPP
        uiDocument.rootVisualElement.AddManipulator(new ChildChangeManipulator());
        uiDocument.rootVisualElement.RegisterCallback<ChildChangeEvent>(evt => childrenChangedEventStream.OnNext(new ChildrenChangedEvent()
        {
            targetParent = evt.targetParent,
            targetChild = evt.targetChild,
            newChildCount = evt.newChildCount,
            previousChildCount = evt.previousChildCount,
        }));
#endif
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

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new();
        bb.Bind(nameof(nextGameRoundInfoUi)).ToExistingInstance(nextGameRoundInfoUi);
        bb.Bind(nameof(nextGameRoundInfoPlayerEntryUi)).ToExistingInstance(nextGameRoundInfoPlayerEntryUi);
        bb.Bind(nameof(micWithNameUi)).ToExistingInstance(micWithNameUi);
        bb.Bind(nameof(songQueueEntryUi)).ToExistingInstance(songQueueEntryUi);
        bb.Bind(nameof(songQueuePlayerEntryUi)).ToExistingInstance(songQueuePlayerEntryUi);
        return bb.GetBindings();
    }
}
