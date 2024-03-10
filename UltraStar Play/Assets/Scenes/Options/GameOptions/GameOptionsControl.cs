using System;
using System.Globalization;
using System.Linq;
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

    private DropdownFieldControl<CultureInfo> languageDropdownFieldControl;

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
        languageDropdownFieldControl = new DropdownFieldControl<CultureInfo>(
            languageDropdownField,
            Translation.GetTranslatedCultureInfos(),
            TranslationConfig.Singleton.CurrentCultureInfo,
            GetCultureInfoDisplayString);
        languageDropdownFieldControl.Selection
            .Subscribe(newValue => OnLanguageChanged(newValue));
    }

    private void OnLanguageChanged(CultureInfo newValue)
    {
        if (Equals(newValue, TranslationConfig.Singleton.CurrentCultureInfo))
        {
            return;
        }
        SetCurrentLanguage(newValue);

        // Reload scene to update translations
        sceneNavigator.LoadScene(EScene.OptionsScene);
    }

    private string GetCultureInfoDisplayString(CultureInfo cultureInfo)
    {
        string suffix = PropertiesFileParser.GetLanguageAndRegionSuffix(cultureInfo).ToLowerInvariant();
        return Translation.Get($"language{suffix}");
    }

    private void SetCurrentLanguage(CultureInfo cultureInfo)
    {
        try
        {
            TranslationConfig.Singleton.CurrentCultureInfo = cultureInfo;
            settings.CultureInfoName = cultureInfo.ToString();
            TranslationManager.ReloadTranslationsAndUpdateScene();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to set current CultureInfo to '{cultureInfo}': {ex.Message}");
        }
    }
}
