using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProTrans;
using UnityEngine;
using UniInject;
using UniRx;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class OptionsUiControl : MonoBehaviour, INeedInjection, ITranslator
{
    [Inject(key = R.UxmlNames.backButtonHashed)]
    private Button backButton;

    [Inject(key = R.UxmlNames.gameOptionsButtonHashed)]
    private Button gameOptionsButton;

    [Inject(key = R.UxmlNames.graphicsOptionsButtonHashed)]
    private Button graphicsOptionsButton;

    [Inject(key = R.UxmlNames.soundOptionsButtonHashed)]
    private Button soundOptionsButton;

    [Inject(key = R.UxmlNames.recordingOptionsButtonHashed)]
    private Button recordingOptionsButton;

    [Inject(key = R.UxmlNames.profileOptionsButtonHashed)]
    private Button profileOptionsButton;

    [Inject(key = R.UxmlNames.designOptionsButtonHashed)]
    private Button designOptionsButton;

    [Inject(key = R.UxmlNames.internetOptionsButtonHashed)]
    private Button internetOptionsButton;

    [Inject(key = R.UxmlNames.appOptionsButtonHashed)]
    private Button appOptionsButton;

    [Inject(key = R.UxmlNames.developerOptionsButtonHashed)]
    private Button developerOptionsButton;

    [Inject(key = R.UxmlNames.languageChooserHashed)]
    private DropdownField languageChooser;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private TranslationManager translationManager;

    [Inject]
    private Settings settings;

	private void Start()
    {
        gameOptionsButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.OptionsGameScene));
        backButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.MainScene));
        gameOptionsButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.OptionsGameScene));
        graphicsOptionsButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.OptionsGraphicsScene));
        soundOptionsButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.OptionsSoundScene));
        recordingOptionsButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.RecordingOptionsScene));
        profileOptionsButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.PlayerProfileSetupScene));
        designOptionsButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.ThemeOptionsScene));
        internetOptionsButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.NetworkOptionsScene));

        InitLanguageChooser();
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
        if (!Application.isPlaying && gameOptionsButton == null)
        {
            SceneInjectionManager.Instance.DoInjection();
        }
        backButton.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.back);
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
