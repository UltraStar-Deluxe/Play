using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using SteamOnlineMultiplayer;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MainSceneControl : MonoBehaviour, INeedInjection, IInjectionFinishedListener, IBinder
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        hasLoggedVersionInfo = false;
        lastSupportTheProjectIconHighlightTimeInSeconds = 0;
    }
    private static bool hasLoggedVersionInfo;

    private const float SupportTheProjectIconHighlightThresholdTimeInSeconds = 60 * 15;
    private static float lastSupportTheProjectIconHighlightTimeInSeconds;

    [InjectedInInspector]
    public TextAsset versionPropertiesTextAsset;

    [InjectedInInspector]
    public VisualTreeAsset newSongDialogUi;

    [InjectedInInspector]
    public VisualTreeAsset connectedClientEntryUi;

    [InjectedInInspector]
    public VisualTreeAsset onlineMultiplayerConnectionDialogUi;

    [InjectedInInspector]
    public CreateSongFromTemplateControl createSongFromTemplateControl;

    [InjectedInInspector]
    public NewVersionChecker newVersionChecker;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private Injector injector;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject(UxmlName = R.UxmlNames.startButton)]
    private Button startButton;

    [Inject(UxmlName = R.UxmlNames.settingsButton)]
    private Button settingsButton;

    [Inject(UxmlName = R.UxmlNames.settingsProblemHintIcon)]
    private VisualElement settingsProblemHintIcon;

    [Inject(UxmlName = R.UxmlNames.aboutButton)]
    private Button aboutButton;

    [Inject(UxmlName = R.UxmlNames.creditsButton)]
    private Button creditsButton;

    [Inject(UxmlName = R.UxmlNames.quitButton)]
    private Button quitButton;

    [Inject(UxmlName = R.UxmlNames.partyButton)]
    private Button partyButton;

    [Inject(UxmlName = R.UxmlNames.createSongButton)]
    private Button createSongButton;

    [Inject(UxmlName = R.UxmlNames.semanticVersionLabel)]
    private Label semanticVersionLabel;

    [Inject(UxmlName = R.UxmlNames.commitHashLabel)]
    private Label commitHashLabel;

    [Inject(UxmlName = R.UxmlNames.buildTimeStampLabel)]
    private Label buildTimeStampLabel;

    [Inject(UxmlName = R.UxmlNames.unityVersionLabel)]
    private Label unityVersionLabel;

    [Inject(UxmlName = R.UxmlNames.versionDetailsContainer)]
    private VisualElement versionDetailsContainer;

    [Inject(UxmlName = R.UxmlNames.logo)]
    private VisualElement logo;

    [Inject(UxmlName = R.UxmlNames.onlineGameButton)]
    private Button onlineGameButton;

    [Inject(UxmlName = R.UxmlNames.supportTheProjectButton)]
    private Button supportTheProjectButton;

    [Inject(UxmlName = R.UxmlNames.supportTheProjectIcon)]
    private VisualElement supportTheProjectIcon;

    [Inject]
    private Settings settings;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private ThemeManager themeManager;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private MicSampleRecorderManager micSampleRecorderManager;

    [Inject]
    private ModManager modManager;

    [Inject]
    private SongIssueManager songIssueManager;

    private MessageDialogControl quitGameDialogControl;
    private NewSongDialogControl newSongDialogControl;
    private OnlineMultiplayerConnectionDialogControl onlineMultiplayerConnectionDialogControl;
    private MessageDialogControl supportTheProjectDialogControl;
    private SettingsProblemHintControl settingsProblemHintControl;
    private readonly BuildInfoUiControl buildInfoUiControl = new();

    public void OnInjectionFinished()
    {
        injector
            .WithBindingForInstance(versionPropertiesTextAsset)
            .Inject(buildInfoUiControl);
    }

    private void Start()
    {
        if (!hasLoggedVersionInfo)
        {
            hasLoggedVersionInfo = true;
            Debug.Log("Version info: " + versionPropertiesTextAsset.text);
        }

        startButton.RegisterCallbackButtonTriggered(_ => OpenSongSelectScene());
        startButton.Focus();
        partyButton.RegisterCallbackButtonTriggered(_ => sceneNavigator.LoadScene(EScene.PartyModeScene));
        onlineGameButton.RegisterCallbackButtonTriggered(_ => OpenOnlineMultiplayerConnectionDialog());
        settingsButton.RegisterCallbackButtonTriggered(_ => sceneNavigator.LoadScene(EScene.OptionsScene));
        aboutButton.RegisterCallbackButtonTriggered(_ => sceneNavigator.LoadScene(EScene.AboutScene));
        creditsButton.RegisterCallbackButtonTriggered(_ => sceneNavigator.LoadScene(EScene.CreditsScene));
        quitButton.RegisterCallbackButtonTriggered(_ => OpenQuitGameDialog());
        createSongButton.RegisterCallbackButtonTriggered(_ => OpenNewSongDialog());
        supportTheProjectButton.RegisterCallbackButtonTriggered(_ => OpenSupportTheProjectDialog());
        HighlightSupportTheProjectButton();

        LeanTween.value(gameObject, 0, 1, 1f)
            .setOnUpdate(value =>
            {
                settingsProblemHintIcon.style.bottom = Length.Percent(value * 100);
                settingsProblemHintIcon.style.scale = new Vector2(value, value);
            })
            .setEaseSpring();

        semanticVersionLabel.RegisterCallback<PointerDownEvent>(_ =>
        {
            versionDetailsContainer.ShowByDisplay();
            StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(10, () => versionDetailsContainer.HideByDisplay()));
            Debug.Log("Version info: " + versionPropertiesTextAsset.text);
            ClipboardUtils.CopyToClipboard(versionPropertiesTextAsset.text);
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_copiedToClipboard));
        });

        InitInputActions();

        settingsProblemHintControl = new SettingsProblemHintControl(
            settingsProblemHintIcon,
            SettingsProblemHintControl.GetAllSettingsProblems(settings, modManager, songIssueManager));

        micSampleRecorderManager.ConnectedMicDevicesChangesStream
            .Subscribe(_ => UpdateSettingsProblemHint())
            .AddTo(gameObject);
    }

    private void Update()
    {
        if (Keyboard.current.leftAltKey.wasPressedThisFrame)
        {
            AnimationUtils.HighlightIconWithBounce(gameObject, supportTheProjectIcon);
        }
    }

    private void HighlightSupportTheProjectButton()
    {
        if (lastSupportTheProjectIconHighlightTimeInSeconds == 0
            || TimeUtils.IsDurationAboveThresholdInSeconds(lastSupportTheProjectIconHighlightTimeInSeconds, SupportTheProjectIconHighlightThresholdTimeInSeconds))
        {
            lastSupportTheProjectIconHighlightTimeInSeconds = Time.time;
            AnimationUtils.HighlightIconWithBounce(gameObject, supportTheProjectIcon);
        }
    }

    private void OpenSupportTheProjectDialog()
    {
        if (supportTheProjectDialogControl != null)
        {
            return;
        }

        supportTheProjectDialogControl = uiManager.CreateDialogControl(Translation.Get(R.Messages.mainScene_supportTheProjectDialog_title));
        supportTheProjectDialogControl.Message = Translation.Get(R.Messages.mainScene_supportTheProjectDialog_message);
        supportTheProjectDialogControl.AddButton(Translation.Get(R.Messages.action_learnMore),
            _ => ApplicationUtils.OpenUrl(Translation.Get(R.Messages.uri_melodyMania)));
        supportTheProjectDialogControl.AddButton(Translation.Get(R.Messages.action_buyOnSteam),
            _ => ApplicationUtils.OpenUrl(Translation.Get(R.Messages.uri_melodyMania_onSteam)));
        supportTheProjectDialogControl.DialogClosedEventStream
            .Subscribe(_ => supportTheProjectDialogControl = null);
    }

    private void OpenOnlineMultiplayerConnectionDialog()
    {
        if (onlineMultiplayerConnectionDialogControl != null)
        {
            return;
        }

        VisualElement dialogVisualElement = onlineMultiplayerConnectionDialogUi.CloneTreeAndGetFirstChild();
        uiDocument.rootVisualElement.Add(dialogVisualElement);

        onlineMultiplayerConnectionDialogControl = injector
            .WithRootVisualElement(dialogVisualElement)
            .CreateAndInject<OnlineMultiplayerConnectionDialogControl>();
        onlineMultiplayerConnectionDialogControl.DialogClosedEventStream
            .Subscribe(_ => onlineMultiplayerConnectionDialogControl = null);
    }

    private void UpdateSettingsProblemHint()
    {
        settingsProblemHintControl.SetProblems(SettingsProblemHintControl.GetAllSettingsProblems(settings, modManager, songIssueManager));
    }

    private void OpenSongSelectScene()
    {
        SongSelectSceneData songSelectSceneData = SceneNavigator.GetSceneData(new SongSelectSceneData());
        songSelectSceneData.partyModeSceneData = null;
        sceneNavigator.LoadScene(EScene.SongSelectScene, songSelectSceneData);
    }

    private void InitInputActions()
    {
        InputManager.GetInputAction(R.InputActions.usplay_start).PerformedAsObservable()
            .Subscribe(_ => OpenSongSelectScene());

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => OnBack());
    }

    public void CloseQuitGameDialog()
    {
        if (quitGameDialogControl == null)
        {
            return;
        }

        quitGameDialogControl.CloseDialog();
        // Must not immediately focus next button or it will trigger as well
        StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(1, () => quitButton.Focus()));
    }

    public void OpenQuitGameDialog()
    {
        if (quitGameDialogControl != null)
        {
            return;
        }

        quitGameDialogControl = uiManager.CreateDialogControl(Translation.Get(R.Messages.mainScene_quitDialog_title));
        quitGameDialogControl.DialogClosedEventStream.Subscribe(_ => quitGameDialogControl = null);
        quitGameDialogControl.Message = Translation.Get(R.Messages.mainScene_quitDialog_message);

        quitGameDialogControl.AddButton(Translation.Get(R.Messages.action_cancel), _ => CloseQuitGameDialog());
        quitGameDialogControl.AddButton(Translation.Get(R.Messages.action_quit), _ => ApplicationUtils.QuitOrStopPlayMode());
    }

    public void OpenNewSongDialog()
    {
        if (newSongDialogControl != null)
        {
            return;
        }

        VisualElement visualElement = newSongDialogUi.CloneTree().Children().FirstOrDefault();
        uiDocument.rootVisualElement.Add(visualElement);

        newSongDialogControl = injector
            .WithRootVisualElement(visualElement)
            .CreateAndInject<NewSongDialogControl>();

        newSongDialogControl.DialogClosedEventStream
            .Subscribe(_ =>
            {
                newSongDialogControl = null;
                createSongButton.Focus();
            });
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new();
        bb.BindExistingInstance(createSongFromTemplateControl);
        bb.BindExistingInstance(gameObject);
        bb.BindExistingInstance(this);
        bb.Bind(nameof(connectedClientEntryUi)).ToExistingInstance(connectedClientEntryUi);
        return bb.GetBindings();
    }

    private void OnBack()
    {
        OpenQuitGameDialog();
    }
}
