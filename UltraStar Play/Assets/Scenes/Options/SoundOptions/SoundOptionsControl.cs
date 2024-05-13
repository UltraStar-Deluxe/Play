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
    private Chooser volumeChooser;

    [Inject(UxmlName = R.UxmlNames.vocalsAudioVolumeChooser)]
    private Chooser vocalsAudioVolumeChooser;

    [Inject(UxmlName = R.UxmlNames.musicVolumeChooser)]
    private Chooser musicVolumeChooser;

    [Inject(UxmlName = R.UxmlNames.previewVolumeChooser)]
    private Chooser previewVolumeChooser;

    [Inject(UxmlName = R.UxmlNames.backgroundMusicVolumeChooser)]
    private Chooser backgroundMusicVolumeChooser;

    [Inject(UxmlName = R.UxmlNames.animateSceneChangeVolumeChooser)]
    private Chooser animateSceneChangeVolumeChooser;

    [Inject(UxmlName = R.UxmlNames.sfxVolumeChooser)]
    private Chooser sfxVolumeChooser;

    [Inject(UxmlName = R.UxmlNames.soundfontPathTextField)]
    private TextField soundfontPathTextField;

    [Inject(UxmlName = R.UxmlNames.testSoundfontButton)]
    private Button testSoundfontButton;

    [Inject(UxmlName = R.UxmlNames.selectSoundfontButton)]
    private Button selectSoundfontButton;

    protected override void Start()
    {
        base.Start();

        PercentNumberChooserControl volumeChooserControl = new(volumeChooser);
        volumeChooserControl.Bind(() => settings.VolumePercent,
            newValue => settings.VolumePercent = (int)newValue);

        PercentNumberChooserControl musicVolumeChooserControl = new(musicVolumeChooser);
        musicVolumeChooserControl.Bind(() => settings.MusicVolumePercent,
            newValue => settings.MusicVolumePercent = (int)newValue);

        PercentNumberChooserControl previewVolumeChooserControl = new(previewVolumeChooser);
        previewVolumeChooserControl.Bind(() => settings.PreviewVolumePercent,
            newValue => settings.PreviewVolumePercent = (int)newValue);

        PercentNumberChooserControl backgroundMusicVolumeChooserControl = new(backgroundMusicVolumeChooser);
        backgroundMusicVolumeChooserControl.Bind(() => settings.BackgroundMusicVolumePercent,
            newValue => settings.BackgroundMusicVolumePercent = (int)newValue);

        // Volume can be changed via REST API
        settings.ObserveEveryValueChanged(it => it.VolumePercent)
            .Subscribe(newValue =>
            {
                if (!volumeChooserControl.Selection.Equals(newValue, 0.1f))
                {
                    volumeChooserControl.Selection = newValue;
                }
            });

        PercentNumberChooserControl animateSceneChangeVolumeChooserControl = new(animateSceneChangeVolumeChooser);
        animateSceneChangeVolumeChooserControl.Bind(() => settings.SceneChangeSoundVolumePercent,
            newValue => settings.SceneChangeSoundVolumePercent = (int)newValue);

        PercentNumberChooserControl sfxVolumeChooserControl = new(sfxVolumeChooser);
        sfxVolumeChooserControl.Bind(() => settings.SfxVolumePercent,
            newValue => settings.SfxVolumePercent = (int)newValue);

        PercentNumberChooserControl vocalsAudioVolumeChooserControl = new(vocalsAudioVolumeChooser);
        vocalsAudioVolumeChooserControl.Bind(() => settings.VocalsAudioVolumePercent,
            newValue => settings.VocalsAudioVolumePercent = (int)newValue);

        soundfontPathTextField.DisableParseEscapeSequences();
        FieldBindingUtils.Bind(gameObject,
            soundfontPathTextField,
            () => settings.SoundfontPath,
            newValue => settings.SoundfontPath = newValue);
        new TextFieldHintControl(soundfontPathTextField);

        testSoundfontButton.RegisterCallbackButtonTriggered(_ => TestSoundfont());
        new TooltipControl(testSoundfontButton, Translation.Get(R.Messages.options_soundFont_test_tooltip), false);

        selectSoundfontButton.RegisterCallbackButtonTriggered(_ => OpenSoundfontDialog());
        new TooltipControl(selectSoundfontButton, Translation.Get(R.Messages.options_soundFont_select_tooltip), false);
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
