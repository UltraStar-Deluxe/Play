using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SoundOptionsControl : AbstractOptionsSceneControl, INeedInjection, ITranslator
{
    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private TranslationManager translationManager;

    [Inject]
    private UIDocument uiDoc;

    [Inject(UxmlName = R.UxmlNames.backgroundMusicVolumeLabel)]
    private Label backgroundMusicVolumeLabel;

    [Inject(UxmlName = R.UxmlNames.backgroundMusicVolumeChooser)]
    private ItemPicker backgroundMusicVolumeChooser;

    [Inject(UxmlName = R.UxmlNames.previewVolumeLabel)]
    private Label previewVolumeLabel;

    [Inject(UxmlName = R.UxmlNames.previewVolumeChooser)]
    private ItemPicker previewVolumeChooser;

    [Inject(UxmlName = R.UxmlNames.volumeLabel)]
    private Label volumeLabel;

    [Inject(UxmlName = R.UxmlNames.volumeChooser)]
    private ItemPicker volumeChooser;

    [Inject(UxmlName = R.UxmlNames.animateSceneChangeVolumePicker)]
    private ItemPicker animateSceneChangeVolumePicker;

    [Inject]
    private Settings settings;

    protected override void Start()
    {
        base.Start();
        
        PercentNumberPickerControl backgroundMusicVolumePickerControl = new(backgroundMusicVolumeChooser);
        backgroundMusicVolumePickerControl.Bind(() => settings.AudioSettings.BackgroundMusicVolumePercent,
            newValue => settings.AudioSettings.BackgroundMusicVolumePercent = (int)newValue);

        PercentNumberPickerControl previewVolumePickerControl = new(previewVolumeChooser);
        previewVolumePickerControl.Bind(() => settings.AudioSettings.PreviewVolumePercent,
            newValue => settings.AudioSettings.PreviewVolumePercent = (int)newValue);

        PercentNumberPickerControl volumePickerControl = new(volumeChooser);
        volumePickerControl.Bind(() => settings.AudioSettings.VolumePercent,
            newValue => settings.AudioSettings.VolumePercent = (int)newValue);

        PercentNumberPickerControl animateSceneChangeVolumePickerControl = new(animateSceneChangeVolumePicker);
        animateSceneChangeVolumePickerControl.Bind(() => settings.AudioSettings.SceneChangeSoundVolumePercent,
            newValue => settings.AudioSettings.SceneChangeSoundVolumePercent = (int)newValue);
    }

    public void UpdateTranslation()
    {
        backgroundMusicVolumeLabel.text = TranslationManager.GetTranslation(R.Messages.options_backgroundMusicEnabled);
        previewVolumeLabel.text = TranslationManager.GetTranslation(R.Messages.options_previewVolume);
        volumeLabel.text = TranslationManager.GetTranslation(R.Messages.options_volume);
    }
}
