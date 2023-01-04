using System;
using System.Collections.Generic;
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
    private static bool wasKickstarterDialogVisible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        wasKickstarterDialogVisible = false;
    }

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

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.sceneSubtitle)]
    private Label sceneSubtitle;

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

    [Inject]
    private Settings settings;

    [Inject]
    private SceneNavigator sceneNavigator;

    private MessageDialogControl kickstarterDialogControl;
    private MessageDialogControl quitGameDialogControl;
    private NewSongDialogControl newSongDialogControl;

    private bool IsNewSongDialogOpen => newSongDialogControl != null;
    private bool IsQuitGameDialogOpen => quitGameDialogControl != null;
    private bool IsKickstarterDialogOpen => kickstarterDialogControl != null;
    private bool IsAnyDialogOpen => IsNewSongDialogOpen || IsQuitGameDialogOpen || IsKickstarterDialogOpen || newVersionChecker.IsNewVersionAvailableDialogOpen;

    private void Start()
    {
        startButton.RegisterCallbackButtonTriggered(() => SceneNavigator.Instance.LoadScene(EScene.SongSelectScene));
        startButton.Focus();
        settingsButton.RegisterCallbackButtonTriggered(() => SceneNavigator.Instance.LoadScene(EScene.OptionsScene));
        aboutButton.RegisterCallbackButtonTriggered(() => SceneNavigator.Instance.LoadScene(EScene.AboutScene));
        creditsButton.RegisterCallbackButtonTriggered(() => SceneNavigator.Instance.LoadScene(EScene.CreditsScene));
        quitButton.RegisterCallbackButtonTriggered(() => OpenQuitGameDialog());
        createSongButton.RegisterCallbackButtonTriggered(() => OpenNewSongDialog());

        InitButtonDescription(startButton, TranslationManager.GetTranslation(R.Messages.mainScene_button_sing_description));
        InitButtonDescription(settingsButton, TranslationManager.GetTranslation(R.Messages.mainScene_button_settings_description));
        InitButtonDescription(aboutButton, TranslationManager.GetTranslation(R.Messages.mainScene_button_about_description));
        InitButtonDescription(creditsButton, TranslationManager.GetTranslation(R.Messages.mainScene_button_credits_description));
        InitButtonDescription(quitButton, TranslationManager.GetTranslation(R.Messages.mainScene_button_quit_description));
        InitButtonDescription(createSongButton, TranslationManager.GetTranslation(R.Messages.mainScene_button_newSong_description));
        InitButtonDescription(partyButton, TranslationManager.GetTranslation(R.Messages.mainScene_button_description_noImplementation));

        UpdateVersionInfoText();

        sceneSubtitle.text = TranslationManager.GetTranslation(R.Messages.mainScene_button_sing_description);

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

    private void Update()
    {
        DateTime kickstarterEnd = new(2022, 12, 31);
        if (!wasKickstarterDialogVisible
            && !IsAnyDialogOpen
            && DateTime.Compare(DateTime.Now, kickstarterEnd) < 0)
        {
            wasKickstarterDialogVisible = true;
            OpenKickstarterDialog();
        }
    }

    private void OpenKickstarterDialog()
    {
        if (kickstarterDialogControl != null)
        {
            return;
        }

        VisualElement visualElement = quitGameDialogUi.CloneTree().Children().FirstOrDefault();
        uiDocument.rootVisualElement.Add(visualElement);

        kickstarterDialogControl = injector
            .WithRootVisualElement(visualElement)
            .CreateAndInject<MessageDialogControl>();
        kickstarterDialogControl.Title = TranslationManager.GetTranslation(R.Messages.mainScene_kickstarterDialog_title);
        kickstarterDialogControl.Message = $"\n{TranslationManager.GetTranslation(R.Messages.mainScene_kickstarterDialog_message)}\n";

        Button visitWebsiteButton = kickstarterDialogControl.AddButton(TranslationManager.GetTranslation(R.Messages.ok), () =>
        {
            Application.OpenURL("https://ultrastar-play.com/kickstarter");
        });
        Button closeButton = kickstarterDialogControl.AddButton(TranslationManager.GetTranslation(R.Messages.close), () => CloseKickstarterDialog());
        closeButton.AddToClassList("transparentBackgroundColor");
        closeButton.AddToClassList("dialogTextButton");
        visitWebsiteButton.Focus();
    }

    private void CloseKickstarterDialog()
    {
        if (kickstarterDialogControl == null)
        {
            return;
        }

        kickstarterDialogControl.CloseDialog();
        kickstarterDialogControl = null;
        startButton.Focus();
    }

    private void InitButtonDescription(Button button, string description)
    {
        button.RegisterCallback<PointerEnterEvent>(_ => sceneSubtitle.text = description);
        button.RegisterCallback<FocusEvent>(_ => sceneSubtitle.text = description);
    }

    public void UpdateTranslation()
    {
        if (!Application.isPlaying && startButton == null)
        {
            SceneInjectionManager.Instance.DoInjection();
        }
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.mainScene_title);
        startButton.text = TranslationManager.GetTranslation(R.Messages.mainScene_button_sing_label);
        partyButton.text = TranslationManager.GetTranslation(R.Messages.mainScene_button_party_label);
        createSongButton.text = TranslationManager.GetTranslation(R.Messages.mainScene_button_newSong_label);
        settingsButton.text = TranslationManager.GetTranslation(R.Messages.mainScene_button_settings_label);
        quitButton.text = TranslationManager.GetTranslation(R.Messages.mainScene_button_quit_label);
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

        StyleSheetControl.Instance.UpdateThemeSpecificStyleSheets();
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
        StyleSheetControl.Instance.UpdateThemeSpecificStyleSheets();

        newSongDialogControl = injector
            .WithRootVisualElement(visualElement)
            .CreateAndInject<NewSongDialogControl>();

        newSongDialogControl.DialogClosedEventStream
            .Subscribe(_ =>
            {
                newSongDialogControl = null;
                createSongButton.Focus();
            });

        StyleSheetControl.Instance.UpdateThemeSpecificStyleSheets();
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
        else if (IsKickstarterDialogOpen)
        {
            CloseKickstarterDialog();
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
