using System;
using System.Linq;
using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class GameOptionsControl : AbstractOptionsSceneControl, INeedInjection, ITranslator
{
    [Inject(UxmlName = R.UxmlNames.scoreModePicker)]
    private ItemPicker scoreModePicker;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.scoreModeContainer)]
    private VisualElement scoreModeContainer;

    [Inject(UxmlName = R.UxmlNames.backButton)]
    private Button backButton;
    
    [Inject(UxmlName = R.UxmlNames.reduceAudioVolumeItemPicker)]
    private ItemPicker reduceAudioVolumeItemPicker;
    
    [Inject(UxmlName = R.UxmlNames.passTheMicTimeItemPicker)]
    private ItemPicker passTheMicTimeItemPicker;
    
    [Inject(UxmlName = R.UxmlNames.languageDropdownField)]
    private DropdownField languageDropdownField;

    protected override void Start()
    {
        base.Start();
        
        new ScoreModeItemPickerControl(scoreModePicker)
            .Bind(() => settings.GameSettings.ScoreMode,
                  newValue => settings.GameSettings.ScoreMode = newValue);

        NumberPickerControl passTheMicTimeItemPickerControl = new NumberPickerControl(passTheMicTimeItemPicker, 20);
        passTheMicTimeItemPickerControl.GetLabelTextFunction = newValue => $"{newValue} s";
        passTheMicTimeItemPickerControl.Bind(
            () => settings.passTheMicTimeInSeconds,
            newValue => settings.passTheMicTimeInSeconds = (int)newValue);

        NumberPickerControl reduceAudioVolumeItemPickerControl = new PercentNumberPickerControl(reduceAudioVolumeItemPicker, 2);
        reduceAudioVolumeItemPickerControl.Bind(
            () => settings.reducedAudioVolumePercent,
            newValue => settings.reducedAudioVolumePercent = (int)newValue);

        backButton.RegisterCallbackButtonTriggered(_ => sceneNavigator.LoadScene(EScene.OptionsScene));
        backButton.Focus();

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => sceneNavigator.LoadScene(EScene.OptionsScene));
    }

    public void UpdateTranslation()
    {
        scoreModePicker.Label = TranslationManager.GetTranslation(R.Messages.options_scoreMode);
    }
    
    private void InitLanguageChooser()
    {
        languageDropdownField.choices = translationManager.GetTranslatedLanguages()
            .Select(languageEnum => languageEnum.ToString())
            .ToList();
        languageDropdownField.value = translationManager.currentLanguage.ToString();

        languageDropdownField.RegisterValueChangedCallback(evt =>
        {
            if (Enum.TryParse(evt.newValue, out SystemLanguage newValue))
            {
                SetLanguage(newValue);
            }
        });
    }

    private void SetLanguage(SystemLanguage newValue)
    {
        if (settings.GameSettings.language == newValue
            && translationManager.currentLanguage == newValue)
        {
            return;
        }

        settings.GameSettings.language = newValue;
        translationManager.currentLanguage = settings.GameSettings.language;
        translationManager.ReloadTranslationsAndUpdateScene();
    }
}
