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
    private BackgroundMusicManager backgroundMusicManager;
    
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

    [Inject(UxmlName = R.UxmlNames.sfxVolumeChooser)]
    private ItemPicker sfxVolumeChooser;
    
    [Inject(UxmlName = R.UxmlNames.soundfontPathTextField)]
    private TextField soundfontPathTextField;

    [Inject(UxmlName = R.UxmlNames.testSoundfontButton)]
    private Button testSoundfontButton;
    
    [Inject(UxmlName = R.UxmlNames.selectSoundfontButton)]
    private Button selectSoundfontButton;
    
    protected override void Start()
    {
        base.Start();
        
        PercentNumberPickerControl backgroundMusicVolumePickerControl = new(backgroundMusicVolumeChooser);
        backgroundMusicVolumePickerControl.Bind(() => settings.BackgroundMusicVolumePercent.Value,
            newValue => settings.BackgroundMusicVolumePercent.Value = (int)newValue);

        PercentNumberPickerControl previewVolumePickerControl = new(previewVolumeChooser);
        previewVolumePickerControl.Bind(() => settings.PreviewVolumePercent.Value,
            newValue => settings.PreviewVolumePercent.Value = (int)newValue);

        PercentNumberPickerControl volumePickerControl = new(volumeChooser);
        volumePickerControl.Bind(() => settings.VolumePercent.Value,
            newValue => settings.VolumePercent.Value = (int)newValue);

        // Volume can be changed via REST API
        settings.VolumePercent
            .Subscribe(newValue =>
            {
                if (!volumePickerControl.SelectedItem.NearlyEquals(newValue, 0.1f))
                {
                    volumePickerControl.SelectItem(newValue);
                }
            });

        PercentNumberPickerControl animateSceneChangeVolumePickerControl = new(animateSceneChangeVolumePicker);
        animateSceneChangeVolumePickerControl.Bind(() => settings.SceneChangeSoundVolumePercent.Value,
            newValue => settings.SceneChangeSoundVolumePercent.Value = (int)newValue);

        PercentNumberPickerControl sfxVolumeChooserControl = new(sfxVolumeChooser);
        sfxVolumeChooserControl.Bind(() => settings.SfxVolumePercent.Value,
            newValue => settings.SfxVolumePercent.Value = (int)newValue);

        PercentNumberPickerControl vocalsAudioVolumePickerControl = new(vocalsAudioVolumeChooser);
        vocalsAudioVolumePickerControl.Bind(() => settings.VocalsAudioVolumePercent.Value,
            newValue => settings.VocalsAudioVolumePercent.Value = (int)newValue);

        FieldBindingUtils.Bind(gameObject,
            soundfontPathTextField,
            () => settings.SoundfontPath.Value,
            newValue => settings.SoundfontPath.Value = newValue);
        new TextFieldHintControl(soundfontPathTextField);
        
        testSoundfontButton.RegisterCallbackButtonTriggered(_ => TestSoundfont());
        selectSoundfontButton.RegisterCallbackButtonTriggered(_ => OpenSoundfontDialog());
    }

    private void OpenSoundfontDialog()
    {
        FileSystemDialogUtils.OpenFileDialogToSetPath(
            "Select Soundfont File",
            "",
            FileSystemDialogUtils.CreateExtensionFilters("Soundfont files", ApplicationUtils.supportedSoundfontFiles),
            () => soundfontPathTextField.value,
            newValue =>
            {
                soundfontPathTextField.value = newValue;
            });
    }

    private void TestSoundfont()
    {
        backgroundMusicManager.BackgroundMusicAudioSource.mute = true;
        MidiFile demoMidiFile = new MidiFile(new StreamingAssetsSoundfontResource(streamingAssetsMidiTestFile));
        midiManager.PlayMidiFile(demoMidiFile);

        float demoMidiFileDurationInSeconds = 4;
        StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(demoMidiFileDurationInSeconds,
            () => backgroundMusicManager.BackgroundMusicAudioSource.mute = false));
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        backgroundMusicManager.BackgroundMusicAudioSource.mute = false;
    }

    public void UpdateTranslation()
    {
        backgroundMusicVolumeChooser.Label = TranslationManager.GetTranslation(R.Messages.options_backgroundMusicEnabled);
        previewVolumeChooser.Label = TranslationManager.GetTranslation(R.Messages.options_previewVolume);
        volumeChooser.Label = TranslationManager.GetTranslation(R.Messages.options_volume);
    }
}
