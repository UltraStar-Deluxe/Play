using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class OptionsOverviewSceneControl : MonoBehaviour, INeedInjection, ITranslator
{
    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.backButton)]
    private Button backButton;

    [Inject(UxmlName = R.UxmlNames.gameOptionsButton)]
    private Button gameOptionsButton;

    [Inject(UxmlName = R.UxmlNames.songsOptionsButton)]
    private Button songsOptionsButton;

    [Inject(UxmlName = R.UxmlNames.graphicsOptionsButton)]
    private Button graphicsOptionsButton;

    [Inject(UxmlName = R.UxmlNames.soundOptionsButton)]
    private Button soundOptionsButton;

    [Inject(UxmlName = R.UxmlNames.recordingOptionsButton)]
    private Button recordingOptionsButton;

    [Inject(UxmlName = R.UxmlNames.profileOptionsButton)]
    private Button profileOptionsButton;

    [Inject(UxmlName = R.UxmlNames.designOptionsButton)]
    private Button designOptionsButton;

    [Inject(UxmlName = R.UxmlNames.internetOptionsButton)]
    private Button internetOptionsButton;

    [Inject(UxmlName = R.UxmlNames.appOptionsButton)]
    private Button appOptionsButton;

    [Inject(UxmlName = R.UxmlNames.developerOptionsButton)]
    private Button developerOptionsButton;

    [Inject(UxmlName = R.UxmlNames.languageChooser)]
    private ItemPicker languageChooser;

    [Inject(UxmlName = R.UxmlNames.songSettingsProblemHintIcon)]
    private VisualElement songSettingsProblemHintIcon;

    [Inject(UxmlName = R.UxmlNames.recordingSettingsProblemHintIcon)]
    private VisualElement recordingSettingsProblemHintIcon;

    [Inject(UxmlName = R.UxmlNames.playerProfileSettingsProblemHintIcon)]
    private VisualElement playerProfileSettingsProblemHintIcon;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private TranslationManager translationManager;

    [Inject]
    private Settings settings;

    [Inject]
    private UIDocument uiDoc;

    [Inject]
    private Injector injector;

	private void Start()
    {
        gameOptionsButton.Focus();

        gameOptionsButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.OptionsGameScene));
        backButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.MainScene));
        songsOptionsButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.SongLibraryOptionsScene));
        graphicsOptionsButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.OptionsGraphicsScene));
        soundOptionsButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.OptionsSoundScene));
        recordingOptionsButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.RecordingOptionsScene));
        profileOptionsButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.PlayerProfileSetupScene));
        designOptionsButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.ThemeOptionsScene));
        internetOptionsButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.NetworkOptionsScene));
        appOptionsButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.CompanionAppOptionsScene));
        developerOptionsButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.DevelopmentOptionsScene));

        InitSettingsProblemHints();
        InitLanguageChooser();

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => sceneNavigator.LoadScene(EScene.MainScene));
    }

    private void InitSettingsProblemHints()
    {
        new SettingsProblemHintControl(
            songSettingsProblemHintIcon,
            SettingsProblemHintControl.GetSongLibrarySettingsProblems(settings),
            injector);

        new SettingsProblemHintControl(
            recordingSettingsProblemHintIcon,
            SettingsProblemHintControl.GetRecordingSettingsProblems(settings),
            injector);

        new SettingsProblemHintControl(
            playerProfileSettingsProblemHintIcon,
            SettingsProblemHintControl.GetPlayerSettingsProblems(settings),
            injector);
    }

    private void InitLanguageChooser()
    {
        new LabeledItemPickerControl<SystemLanguage>(
                languageChooser,
                translationManager.GetTranslatedLanguages())
            .Bind(() => translationManager.currentLanguage,
                newValue => SetLanguage(newValue));
    }

    public void UpdateTranslation()
    {
        if (!Application.isPlaying && backButton == null)
        {
            SceneInjectionManager.Instance.DoInjection();
        }

        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.options);
        backButton.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.back);
        gameOptionsButton.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_game_button);
        songsOptionsButton.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_songLibrary_button);
        soundOptionsButton.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_sound_button);
        graphicsOptionsButton.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_graphics_button);
        recordingOptionsButton.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_recording_button);
        profileOptionsButton.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_playerProfiles_button);
        designOptionsButton.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_design_button);
        internetOptionsButton.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_internet_button);
        appOptionsButton.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_companionApp_button);
        developerOptionsButton.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_development_button);
    }

    private void SetLanguage(SystemLanguage newValue)
    {
        settings.GameSettings.language = newValue;
        translationManager.currentLanguage = settings.GameSettings.language;
        translationManager.ReloadTranslationsAndUpdateScene();
    }
}
