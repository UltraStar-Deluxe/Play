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

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SoundOptionsUiControl : MonoBehaviour, INeedInjection, ITranslator
{
    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private TranslationManager translationManager;

    [Inject]
    private UIDocument uiDoc;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.backgroundMusicEnabledLabel)]
    private Label backgroundMusicEnabledLabel;

    [Inject(UxmlName = R.UxmlNames.backgroundMusicEnabledChooser)]
    private ItemPicker backgroundMusicEnabledChooser;

    [Inject(UxmlName = R.UxmlNames.previewVolumeLabel)]
    private Label previewVolumeLabel;

    [Inject(UxmlName = R.UxmlNames.previewVolumeChooser)]
    private ItemPicker previewVolumeChooser;

    [Inject(UxmlName = R.UxmlNames.volumeLabel)]
    private Label volumeLabel;

    [Inject(UxmlName = R.UxmlNames.volumeChooser)]
    private ItemPicker volumeChooser;

    [Inject(UxmlName = R.UxmlNames.backButton)]
    private Button backButton;

    [Inject]
    private Settings settings;

    private void Start()
    {
        uiDoc.rootVisualElement.Query<Button>().ForEach(button => button.focusable = true);

        new BoolPickerControl(backgroundMusicEnabledChooser)
            .Bind(() => settings.AudioSettings.BackgroundMusicEnabled,
                  newValue => settings.AudioSettings.BackgroundMusicEnabled = newValue);

        new PercentNumberPickerControl(previewVolumeChooser)
            .Bind(() => settings.AudioSettings.PreviewVolumePercent,
                newValue => settings.AudioSettings.PreviewVolumePercent = (int)newValue);

        new PercentNumberPickerControl(volumeChooser)
            .Bind(() => settings.AudioSettings.VolumePercent,
                newValue => settings.AudioSettings.VolumePercent = (int)newValue);

        backButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.OptionsScene));

        backgroundMusicEnabledChooser.PreviousItemButton.Focus();

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => sceneNavigator.LoadScene(EScene.OptionsScene));
    }

    public void UpdateTranslation()
    {
        if (!Application.isPlaying && backgroundMusicEnabledLabel == null)
        {
            SceneInjectionManager.Instance.DoInjection();
        }
        backgroundMusicEnabledLabel.text = TranslationManager.GetTranslation(R.Messages.options_backgroundMusicEnabled);
        previewVolumeLabel.text = TranslationManager.GetTranslation(R.Messages.options_previewVolume);
        volumeLabel.text = TranslationManager.GetTranslation(R.Messages.options_volume);
        backButton.text = TranslationManager.GetTranslation(R.Messages.back);
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.soundOptionsScene_title);
    }
}
