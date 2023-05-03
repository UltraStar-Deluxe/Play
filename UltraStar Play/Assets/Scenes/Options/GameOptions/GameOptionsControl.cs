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

public class GameOptionsControl : AbstractOptionsSceneControl, INeedInjection
{
    [Inject(UxmlName = R.UxmlNames.reduceAudioVolumeItemPicker)]
    private ItemPicker reduceAudioVolumeItemPicker;
    
    [Inject(UxmlName = R.UxmlNames.passTheMicTimeItemPicker)]
    private ItemPicker passTheMicTimeItemPicker;
    
    [Inject(UxmlName = R.UxmlNames.languageDropdownField)]
    private DropdownField languageDropdownField;
    
    [Inject(UxmlName = R.UxmlNames.defaultMedleyTargetDurationPicker)]
    private ItemPicker defaultMedleyTargetDurationPicker;

    protected override void Start()
    {
        base.Start();
        
        NumberPickerControl passTheMicTimeItemPickerControl = new NumberPickerControl(passTheMicTimeItemPicker, 20);
        passTheMicTimeItemPickerControl.GetLabelTextFunction = newValue => $"{newValue} s";
        passTheMicTimeItemPickerControl.Bind(
            () => settings.PassTheMicTimeInSeconds,
            newValue => settings.PassTheMicTimeInSeconds = (int)newValue);

        NumberPickerControl reduceAudioVolumeItemPickerControl = new PercentNumberPickerControl(reduceAudioVolumeItemPicker, 2);
        reduceAudioVolumeItemPickerControl.Bind(
            () => settings.ReducedAudioVolumePercent,
            newValue => settings.ReducedAudioVolumePercent = (int)newValue);

        NumberPickerControl defaultMedleyDurationPickerControl = new NumberPickerControl(defaultMedleyTargetDurationPicker, 30);
        defaultMedleyDurationPickerControl.GetLabelTextFunction = newValue => $"{newValue} s";
        defaultMedleyDurationPickerControl.Bind(
            () => settings.DefaultMedleyTargetDurationInSeconds,
            newValue => settings.DefaultMedleyTargetDurationInSeconds = (int)newValue);
        
        InitLanguageChooser();
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
        if (settings.Language == newValue
            && translationManager.currentLanguage == newValue)
        {
            return;
        }

        settings.Language = newValue;
        translationManager.currentLanguage = settings.Language;
        translationManager.ReloadTranslationsAndUpdateScene();
    }
}
