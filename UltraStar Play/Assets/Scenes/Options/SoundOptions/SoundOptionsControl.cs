using AudioSynthesis.Midi;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SoundOptionsControl : AbstractOptionsSceneControl, INeedInjection
{
    private static readonly string streamingAssetsMidiTestFile = "Midi/fur-elise-beginning.mid";

    [Inject]
    private MidiManager midiManager;

    [Inject]
    private BackgroundMusicManager backgroundMusicManager;

    [Inject]
    private UIDocument uiDoc;

    [Inject(UxmlName = R.UxmlNames.volumeChooser)]
    private ItemPicker volumeChooser;

    [Inject(UxmlName = R.UxmlNames.vocalsAudioVolumeChooser)]
    private ItemPicker vocalsAudioVolumeChooser;

    [Inject(UxmlName = R.UxmlNames.musicVolumeChooser)]
    private ItemPicker musicVolumeChooser;

    [Inject(UxmlName = R.UxmlNames.previewVolumeChooser)]
    private ItemPicker previewVolumeChooser;

    [Inject(UxmlName = R.UxmlNames.backgroundMusicVolumeChooser)]
    private ItemPicker backgroundMusicVolumeChooser;

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

        PercentNumberPickerControl volumePickerControl = new(volumeChooser);
        volumePickerControl.Bind(() => settings.VolumePercent,
            newValue => settings.VolumePercent = (int)newValue);

        PercentNumberPickerControl musicVolumePickerControl = new(musicVolumeChooser);
        musicVolumePickerControl.Bind(() => settings.MusicVolumePercent,
            newValue => settings.MusicVolumePercent = (int)newValue);

        PercentNumberPickerControl previewVolumePickerControl = new(previewVolumeChooser);
        previewVolumePickerControl.Bind(() => settings.PreviewVolumePercent,
            newValue => settings.PreviewVolumePercent = (int)newValue);

        PercentNumberPickerControl backgroundMusicVolumePickerControl = new(backgroundMusicVolumeChooser);
        backgroundMusicVolumePickerControl.Bind(() => settings.BackgroundMusicVolumePercent,
            newValue => settings.BackgroundMusicVolumePercent = (int)newValue);

        // Volume can be changed via REST API
        settings.ObserveEveryValueChanged(it => it.VolumePercent)
            .Subscribe(newValue =>
            {
                if (!volumePickerControl.SelectedItem.NearlyEquals(newValue, 0.1f))
                {
                    volumePickerControl.SelectItem(newValue);
                }
            });

        PercentNumberPickerControl animateSceneChangeVolumePickerControl = new(animateSceneChangeVolumePicker);
        animateSceneChangeVolumePickerControl.Bind(() => settings.SceneChangeSoundVolumePercent,
            newValue => settings.SceneChangeSoundVolumePercent = (int)newValue);

        PercentNumberPickerControl sfxVolumeChooserControl = new(sfxVolumeChooser);
        sfxVolumeChooserControl.Bind(() => settings.SfxVolumePercent,
            newValue => settings.SfxVolumePercent = (int)newValue);

        PercentNumberPickerControl vocalsAudioVolumePickerControl = new(vocalsAudioVolumeChooser);
        vocalsAudioVolumePickerControl.Bind(() => settings.VocalsAudioVolumePercent,
            newValue => settings.VocalsAudioVolumePercent = (int)newValue);

        soundfontPathTextField.DisableParseEscapeSequences();
        FieldBindingUtils.Bind(gameObject,
            soundfontPathTextField,
            () => settings.SoundfontPath,
            newValue => settings.SoundfontPath = newValue);
        new TextFieldHintControl(soundfontPathTextField);

        testSoundfontButton.RegisterCallbackButtonTriggered(_ => TestSoundfont());
        new TooltipControl(testSoundfontButton, "Test soundfont", false);

        selectSoundfontButton.RegisterCallbackButtonTriggered(_ => OpenSoundfontDialog());
        new TooltipControl(selectSoundfontButton, "Select soundfont", false);
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
}
