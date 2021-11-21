using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using ProTrans;
using UnityEngine;
using UniInject;
using UniRx;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class OptionsOverviewSceneUiControl : MonoBehaviour, INeedInjection, ITranslator
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
    private DropdownField languageChooser;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private TranslationManager translationManager;

    [Inject]
    private Settings settings;

    [Inject]
    private UIDocument uiDoc;

	private void Start()
    {
        uiDoc.rootVisualElement.Query<Button>().ForEach(button => button.focusable = true);
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

        InitLanguageChooser();

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => sceneNavigator.LoadScene(EScene.MainScene));
    }

    private void InitLanguageChooser()
    {
        languageChooser.choices = translationManager.GetTranslatedLanguages()
            .Select(lang => lang.ToString())
            .ToList();
        languageChooser.SetValueWithoutNotify(settings.GameSettings.language.ToString());
        languageChooser.RegisterValueChangedCallback(evt => SetLanguage(evt.newValue));
    }

    public void UpdateTranslation()
    {
        if (!Application.isPlaying && backButton == null)
        {
            SceneInjectionManager.Instance.DoInjection();
        }

        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.options);
        backButton.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.back);
        gameOptionsButton.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.optionsScene_button_game);
        songsOptionsButton.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.optionsScene_button_songs);
        graphicsOptionsButton.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.optionsScene_button_graphics);
        recordingOptionsButton.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.optionsScene_button_recording);
        profileOptionsButton.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.optionsScene_button_profiles);
        designOptionsButton.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.optionsScene_button_design);
        internetOptionsButton.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.optionsScene_button_internet);
        appOptionsButton.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.optionsScene_button_apps);
        developerOptionsButton.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.optionsScene_button_development);
    }

    private void SetLanguage(string newLanguageString)
    {
        if (Enum.TryParse(newLanguageString, true, out SystemLanguage newLanguageEnum))
        {
            settings.GameSettings.language = newLanguageEnum;
            translationManager.currentLanguage = settings.GameSettings.language;
            translationManager.ReloadTranslationsAndUpdateScene();
        }
    }
}