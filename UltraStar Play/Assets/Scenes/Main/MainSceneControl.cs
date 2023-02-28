using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MainSceneControl : MonoBehaviour, INeedInjection, ITranslator, IBinder
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        hasLoggedVersionInfo = false;
    }
    private static bool hasLoggedVersionInfo;
    
    [InjectedInInspector]
    public TextAsset versionPropertiesTextAsset;

    [InjectedInInspector]
    public VisualTreeAsset quitGameDialogUi;

    [InjectedInInspector]
    public VisualTreeAsset newSongDialogUi;

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

    [Inject(UxmlName = R.UxmlNames.semanticVersionText)]
    private Label semanticVersionText;
    
    [Inject(UxmlName = R.UxmlNames.commitHashText)]
    private Label commitHashText;
    
    [Inject(UxmlName = R.UxmlNames.buildTimeStampText)]
    private Label buildTimeStampText;

    [Inject(UxmlName = R.UxmlNames.versionDetailsContainer)]
    private VisualElement versionDetailsContainer;
    
    [Inject]
    private Settings settings;

    [Inject]
    private SceneNavigator sceneNavigator;

    private MessageDialogControl quitGameDialogControl;
    private NewSongDialogControl newSongDialogControl;

    private bool IsNewSongDialogOpen => newSongDialogControl != null;
    private bool IsQuitGameDialogOpen => quitGameDialogControl != null;
    private bool IsAnyDialogOpen => IsNewSongDialogOpen || IsQuitGameDialogOpen || newVersionChecker.IsNewVersionAvailableDialogOpen;

    private void Start()
    {
        if (!hasLoggedVersionInfo)
        {
            hasLoggedVersionInfo = true;
            Debug.Log("Version info: " + versionPropertiesTextAsset.text);
        }

        startButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.SongSelectScene));
        startButton.Focus();
        settingsButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.OptionsScene));
        aboutButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.AboutScene));
        creditsButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.CreditsScene));
        quitButton.RegisterCallbackButtonTriggered(() => OpenQuitGameDialog());
        createSongButton.RegisterCallbackButtonTriggered(() => OpenNewSongDialog());

        LeanTween.value(gameObject, 0, 1, 1f)
            .setOnUpdate(value =>
            {
                settingsProblemHintIcon.style.bottom = Length.Percent(value * 100);
                settingsProblemHintIcon.style.scale = new Vector2(value, value);
            })
            .setEaseSpring();

        semanticVersionText.RegisterCallback<PointerDownEvent>(_ =>
        {
            versionDetailsContainer.ShowByDisplay();
            StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(10, () => versionDetailsContainer.HideByDisplay()));
            Debug.Log("Version info: " + versionPropertiesTextAsset.text);
        });

        InitButtonDescription(startButton, TranslationManager.GetTranslation(R.Messages.mainScene_button_sing_description));
        InitButtonDescription(settingsButton, TranslationManager.GetTranslation(R.Messages.mainScene_button_settings_description));
        InitButtonDescription(aboutButton, TranslationManager.GetTranslation(R.Messages.mainScene_button_about_description));
        InitButtonDescription(creditsButton, TranslationManager.GetTranslation(R.Messages.mainScene_button_credits_description));
        InitButtonDescription(quitButton, TranslationManager.GetTranslation(R.Messages.mainScene_button_quit_description));
        InitButtonDescription(createSongButton, TranslationManager.GetTranslation(R.Messages.mainScene_button_newSong_description));
        InitButtonDescription(partyButton, TranslationManager.GetTranslation(R.Messages.mainScene_button_description_noImplementation));

        UpdateVersionInfoText();

        // sceneSubtitle.text = TranslationManager.GetTranslation(R.Messages.mainScene_button_sing_description);

        InitInputActions();

        songMetaManager.ScanFilesIfNotDoneYet();

        new SettingsProblemHintControl(
            settingsProblemHintIcon,
            SettingsProblemHintControl.GetAllSettingsProblems(settings, songMetaManager),
            injector);
    }

    private void InitInputActions()
    {
        InputManager.GetInputAction(R.InputActions.usplay_start).PerformedAsObservable()
            .Subscribe(_ => sceneNavigator.LoadScene(EScene.SongSelectScene));

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => OnBack());
    }

    private void InitButtonDescription(Button button, string description)
    {
        // button.RegisterCallback<PointerEnterEvent>(_ => sceneSubtitle.text = description);
        // button.RegisterCallback<FocusEvent>(_ => sceneSubtitle.text = description);
    }

    public void UpdateTranslation()
    {
        // sceneTitle.text = TranslationManager.GetTranslation(R.Messages.mainScene_title);
        startButton.text = TranslationManager.GetTranslation(R.Messages.mainScene_button_sing_label);
        partyButton.text = TranslationManager.GetTranslation(R.Messages.mainScene_button_party_label);
    }

    private void UpdateVersionInfoText()
    {
        Dictionary<string, string> versionProperties = PropertiesFileParser.ParseText(versionPropertiesTextAsset.text);

        // Show the release number (e.g. release date, or some version number)
        versionProperties.TryGetValue("release", out string release);
        versionProperties.TryGetValue("name", out string releaseName);
        string displayName = releaseName.IsNullOrEmpty() ? release : releaseName;
        semanticVersionText.text = "Version: " + displayName;

        // Show the commit hash of the build
        versionProperties.TryGetValue("commit_hash", out string commitHash);
        commitHashText.text = "Commit: " + commitHash;
        
        // Show the build timestamp only for development builds
        if (Debug.isDebugBuild)
        {
            versionProperties.TryGetValue("build_timestamp", out string buildTimeStamp);
            buildTimeStampText.text = "Build timestamp: " + buildTimeStamp;
        }
        else
        {
            buildTimeStampText.text = "";
            buildTimeStampText.HideByDisplay();
        }
    }

    public void CloseQuitGameDialog()
    {
        if (quitGameDialogControl == null)
        {
            return;
        }

        quitGameDialogControl.CloseDialog();
        quitGameDialogControl = null;
        // Must not immediately focus next button or it will trigger as well
        StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(1, () => quitButton.Focus()));
    }

    public void OpenQuitGameDialog()
    {
        if (quitGameDialogControl != null)
        {
            return;
        }

        VisualElement visualElement = quitGameDialogUi.CloneTree().Children().FirstOrDefault();
        uiDocument.rootVisualElement.Add(visualElement);

        quitGameDialogControl = injector
            .WithRootVisualElement(visualElement)
            .CreateAndInject<MessageDialogControl>();
        quitGameDialogControl.Title = TranslationManager.GetTranslation(R.Messages.mainScene_quitDialog_title);
        quitGameDialogControl.Message = $"\n{TranslationManager.GetTranslation(R.Messages.mainScene_quitDialog_message)}\n";

        quitGameDialogControl.AddButton(TranslationManager.GetTranslation(R.Messages.no), () => CloseQuitGameDialog());
        Button yesButton = quitGameDialogControl.AddButton(TranslationManager.GetTranslation(R.Messages.yes), () => ApplicationUtils.QuitOrStopPlayMode());
        yesButton.Focus();

        ThemeManager.ApplyThemeSpecificStylesToVisualElementsInScene();
    }

    public void OpenNewSongDialog()
    {
        if (newSongDialogControl != null)
        {
            return;
        }

        VisualElement visualElement = newSongDialogUi.CloneTree().Children().FirstOrDefault();
        uiDocument.rootVisualElement.Add(visualElement);
        // TODO would be nice to find a way to automatize calling this method when a new dialog/visual element/etc. is spawned
        ThemeManager.ApplyThemeSpecificStylesToVisualElementsInScene();

        newSongDialogControl = injector
            .WithRootVisualElement(visualElement)
            .CreateAndInject<NewSongDialogControl>();

        newSongDialogControl.DialogClosedEventStream
            .Subscribe(_ =>
            {
                newSongDialogControl = null;
                createSongButton.Focus();
            });

        ThemeManager.ApplyThemeSpecificStylesToVisualElementsInScene();
    }

    public void CloseNewSongDialog()
    {
        if (newSongDialogControl == null)
        {
            return;
        }

        newSongDialogControl.CloseDialog();
        newSongDialogControl = null;
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new();
        bb.BindExistingInstance(createSongFromTemplateControl);
        bb.BindExistingInstance(gameObject);
        bb.BindExistingInstance(this);
        return bb.GetBindings();
    }

    private void OnBack()
    {
        if (IsNewSongDialogOpen)
        {
            CloseNewSongDialog();
        }
        else if (IsQuitGameDialogOpen)
        {
            CloseQuitGameDialog();
        }
        else if (newVersionChecker.IsNewVersionAvailableDialogOpen)
        {
            newVersionChecker.CloseNewVersionAvailableDialog();
        }
        else
        {
            OpenQuitGameDialog();
        }
    }
}
