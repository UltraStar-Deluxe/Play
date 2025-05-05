using System;
using System.Globalization;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class GameOptionsSceneControl : AbstractOptionsSceneControl, INeedInjection
{
    [Inject(UxmlName = R.UxmlNames.reduceAudioVolumeChooser)]
    private Chooser reduceAudioVolumeChooser;

    [Inject(UxmlName = R.UxmlNames.passTheMicTimeChooser)]
    private Chooser passTheMicTimeChooser;

    [Inject(UxmlName = R.UxmlNames.skipToNextLyricsTimeChooser)]
    private Chooser skipToNextLyricsTimeChooser;

    [Inject(UxmlName = R.UxmlNames.languageDropdownField)]
    private DropdownField languageDropdownField;

    [Inject(UxmlName = R.UxmlNames.defaultMedleyTargetDurationChooser)]
    private Chooser defaultMedleyTargetDurationChooser;

    protected override void Start()
    {
        base.Start();

        NumberChooserControl skipToNextLyricsTimeChooserControl = new NumberChooserControl(skipToNextLyricsTimeChooser, 20);
        skipToNextLyricsTimeChooserControl.GetLabelTextFunction = newValue => $"{newValue} s";
        skipToNextLyricsTimeChooserControl.Bind(
            () => settings.SkipToNextLyricsTimeInSeconds,
            newValue => settings.SkipToNextLyricsTimeInSeconds = (int)newValue);

        NumberChooserControl passTheMicTimeChooserControl = new NumberChooserControl(passTheMicTimeChooser, 20);
        passTheMicTimeChooserControl.GetLabelTextFunction = newValue => $"{newValue} s";
        passTheMicTimeChooserControl.Bind(
            () => settings.PassTheMicTimeInSeconds,
            newValue => settings.PassTheMicTimeInSeconds = (int)newValue);

        NumberChooserControl reduceAudioVolumeChooserControl = new PercentNumberChooserControl(reduceAudioVolumeChooser, 2);
        reduceAudioVolumeChooserControl.Bind(
            () => settings.ReducedAudioVolumePercent,
            newValue => settings.ReducedAudioVolumePercent = (int)newValue);

        NumberChooserControl defaultMedleyDurationChooserControl = new NumberChooserControl(defaultMedleyTargetDurationChooser, 30);
        defaultMedleyDurationChooserControl.GetLabelTextFunction = newValue => $"{newValue} s";
        defaultMedleyDurationChooserControl.Bind(
            () => settings.DefaultMedleyTargetDurationInSeconds,
            newValue => settings.DefaultMedleyTargetDurationInSeconds = (int)newValue);

        LanguageChooserControl languageChooserControl = new LanguageChooserControl(languageDropdownField);
        languageChooserControl.SelectionAsObservable.Subscribe(newValue => OnLanguageChanged(newValue));
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

    private void SetCurrentLanguage(CultureInfo cultureInfo)
    {
        try
        {
            TranslationConfig.Singleton.CurrentCultureInfo = cultureInfo;
            settings.CultureInfoName = cultureInfo.ToString();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to set current CultureInfo to '{cultureInfo}': {ex.Message}");
        }
    }
}
