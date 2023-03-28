using AudioSynthesis.Midi;
using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using UnityMidi;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SoundOptionsControl : AbstractOptionsSceneControl, INeedInjection, ITranslator
{
    private static readonly string streamingAssetsMidiTestFile = "Midi/fur-elise-beginning.mid";
    
    [Inject]
    private MidiManager midiManager;
    
    [Inject]
    private UIDocument uiDoc;

    [Inject(UxmlName = R.UxmlNames.backgroundMusicVolumeChooser)]
    private ItemPicker backgroundMusicVolumeChooser;

    [Inject(UxmlName = R.UxmlNames.previewVolumeChooser)]
    private ItemPicker previewVolumeChooser;

    [Inject(UxmlName = R.UxmlNames.vocalsAudioVolumeChooser)]
    private ItemPicker vocalsAudioVolumeChooser;

    [Inject(UxmlName = R.UxmlNames.volumeChooser)]
    private ItemPicker volumeChooser;

    [Inject(UxmlName = R.UxmlNames.animateSceneChangeVolumePicker)]
    private ItemPicker animateSceneChangeVolumePicker;

    [Inject(UxmlName = R.UxmlNames.soundfontPathTextField)]
    private TextField soundfontPathTextField;

    [Inject(UxmlName = R.UxmlNames.testSoundfontButton)]
    private Button testSoundfontButton;
    
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

        // Volume can be changed via REST API
        settings.ObserveEveryValueChanged(it => it.AudioSettings.VolumePercent)
            .Subscribe(newValue =>
            {
                if (!volumePickerControl.SelectedItem.NearlyEquals(newValue, 0.1f))
                {
                    volumePickerControl.SelectItem(newValue);
                }
            });

        PercentNumberPickerControl animateSceneChangeVolumePickerControl = new(animateSceneChangeVolumePicker);
        animateSceneChangeVolumePickerControl.Bind(() => settings.AudioSettings.SceneChangeSoundVolumePercent,
            newValue => settings.AudioSettings.SceneChangeSoundVolumePercent = (int)newValue);

        PercentNumberPickerControl vocalsAudioVolumePickerControl = new(vocalsAudioVolumeChooser);
        vocalsAudioVolumePickerControl.Bind(() => settings.AudioSettings.VocalsAudioVolumePercent,
            newValue => settings.AudioSettings.VocalsAudioVolumePercent = (int)newValue);

        FieldBindingUtils.Bind(gameObject,
            soundfontPathTextField,
            () => settings.AudioSettings.soundfontPath,
            newValue => settings.AudioSettings.soundfontPath = newValue);
        
        testSoundfontButton.RegisterCallbackButtonTriggered(_ => TestSoundfont());
        
        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => sceneNavigator.LoadScene(EScene.OptionsScene));
    }

    private void TestSoundfont()
    {
        midiManager.PlayMidiFile(new MidiFile(new StreamingAssetResource(streamingAssetsMidiTestFile)));
    }

    public void UpdateTranslation()
    {
        backgroundMusicVolumeChooser.Label = TranslationManager.GetTranslation(R.Messages.options_backgroundMusicEnabled);
        previewVolumeChooser.Label = TranslationManager.GetTranslation(R.Messages.options_previewVolume);
        volumeChooser.Label = TranslationManager.GetTranslation(R.Messages.options_volume);
    }
}
