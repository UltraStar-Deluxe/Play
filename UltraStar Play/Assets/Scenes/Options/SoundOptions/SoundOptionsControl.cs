using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SoundOptionsControl : MonoBehaviour, INeedInjection, ITranslator
{
    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private TranslationManager translationManager;

    [Inject]
    private UIDocument uiDoc;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

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

    [Inject(UxmlName = R.UxmlNames.backButton)]
    private Button backButton;

    [Inject]
    private Settings settings;

    private void Start()
    {
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

        backButton.RegisterCallbackButtonTriggered(() => sceneNavigator.LoadScene(EScene.OptionsScene));
        backButton.Focus();

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => sceneNavigator.LoadScene(EScene.OptionsScene));
    }

    public void UpdateTranslation()
    {
        if (!Application.isPlaying && backButton == null)
        {
            UltraStarPlaySceneInjectionManager.Instance.DoInjection();
        }
        backgroundMusicVolumeLabel.text = TranslationManager.GetTranslation(R.Messages.options_backgroundMusicEnabled);
        previewVolumeLabel.text = TranslationManager.GetTranslation(R.Messages.options_previewVolume);
        volumeLabel.text = TranslationManager.GetTranslation(R.Messages.options_volume);
        backButton.text = TranslationManager.GetTranslation(R.Messages.back);
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.options_sound_title);
    }
}
