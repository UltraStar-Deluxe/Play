using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class SongEditorSideBarSettingsControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = R.UxmlNames.adjustFollowingNotesToggle)]
    private Toggle adjustFollowingNotesToggle;

    [Inject(UxmlName = R.UxmlNames.autoSaveToggle)]
    private Toggle autoSaveToggle;

    [Inject(UxmlName = R.UxmlNames.goToLastPlaybackPositionToggle)]
    private Toggle goToLastPlaybackPositionToggle;

    [Inject(UxmlName = R.UxmlNames.musicVolumeSlider)]
    private Slider musicVolumeSlider;

    [Inject(UxmlName = R.UxmlNames.musicPlaybackSpeedSlider)]
    private Slider musicPlaybackSpeedSlider;

    [Inject(UxmlName = R.UxmlNames.resetMusicPlaybackSpeedButton)]
    private Button resetMusicPlaybackSpeedButton;

    [Inject(UxmlName = R.UxmlNames.recordingSourceItemPicker)]
    private ItemPicker recordingSourceItemPicker;
    private LabeledItemPickerControl<ESongEditorRecordingSource> recordingSourceItemPickerControl;

    [Inject(UxmlName = R.UxmlNames.micDeviceItemPicker)]
    private ItemPicker micDeviceItemPicker;
    private LabeledItemPickerControl<MicProfile> micDeviceItemPickerControl;

    [Inject(UxmlName = R.UxmlNames.micOctaveOffsetTextField)]
    private TextField micOctaveOffsetTextField;

    [Inject(UxmlName = R.UxmlNames.micDelayTextField)]
    private TextField micDelayTextField;

    [Inject(UxmlName = R.UxmlNames.buttonRecordingPitchTextField)]
    private TextField buttonRecordingPitchTextField;

    [Inject(UxmlName = R.UxmlNames.buttonRecordingButtonTextField)]
    private TextField buttonRecordingButtonTextField;

    [Inject(UxmlName = R.UxmlNames.midiGainSlider)]
    private Slider midiGainSlider;

    [Inject(UxmlName = R.UxmlNames.midiVelocitySlider)]
    private Slider midiVelocitySlider;

    [Inject(UxmlName = R.UxmlNames.midiDelayTextField)]
    private TextField midiDelayTextField;

    [Inject(UxmlName = R.UxmlNames.midiNotePlayAlongToggle)]
    private Toggle midiNotePlayAlongToggle;

    [Inject(UxmlName = R.UxmlNames.showLyricsAreaToggle)]
    private Toggle showLyricsAreaToggle;

    [Inject(UxmlName = R.UxmlNames.showStatusBarToggle)]
    private Toggle showStatusBarToggle;

    [Inject(UxmlName = R.UxmlNames.showControlHintsToggle)]
    private Toggle showControlHintsToggle;

    [Inject(UxmlName = R.UxmlNames.showVideoAreaToggle)]
    private Toggle showVideoAreaToggle;

    [Inject(UxmlName = R.UxmlNames.showVirtualPianoToggle)]
    private Toggle showVirtualPianoToggle;

    [Inject(UxmlName = R.UxmlNames.gridSizeTextField)]
    private TextField gridSizeTextField;

    [Inject(UxmlName = R.UxmlNames.sentenceLineSizeTextField)]
    private TextField sentenceLineSizeTextField;

    [Inject(UxmlName = R.UxmlNames.videoArea)]
    private VisualElement videoArea;

    [Inject(UxmlName = R.UxmlNames.statusBar)]
    private VisualElement statusBar;

    [Inject(UxmlName = R.UxmlNames.virtualPiano)]
    private VisualElement virtualPiano;

    [Inject(UxmlName = R.UxmlNames.lyricsArea)]
    private VisualElement lyricsArea;

    [Inject(UxmlName = R.UxmlNames.micRecordingContainer)]
    private VisualElement micRecordingContainer;

    [Inject(UxmlName = R.UxmlNames.buttonRecordingContainer)]
    private VisualElement buttonRecordingContainer;

    [Inject(UxmlName = R.UxmlNames.importMidiFileButton)]
    private Button importMidiFileButton;

    [Inject]
    private Settings settings;

    [Inject]
    private GameObject gameObject;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private Injector injector;

    private readonly SongEditorMidiFileImporter midiFileImporter = new();

    public void OnInjectionFinished()
    {
        injector.Inject(midiFileImporter);

        // Editing settings
        Bind(adjustFollowingNotesToggle,
            () => settings.SongEditorSettings.AdjustFollowingNotes,
            newValue => settings.SongEditorSettings.AdjustFollowingNotes = newValue);

        Bind(autoSaveToggle,
            () => settings.SongEditorSettings.AutoSave,
            newValue => settings.SongEditorSettings.AutoSave = newValue);

        // Music settings
        Bind(goToLastPlaybackPositionToggle,
            () => settings.SongEditorSettings.GoToLastPlaybackPosition,
            newValue => settings.SongEditorSettings.GoToLastPlaybackPosition = newValue);

        Bind(musicVolumeSlider,
            () => settings.AudioSettings.VolumePercent,
            newValue => settings.AudioSettings.VolumePercent = (int) newValue);

        // Playback speed
        songAudioPlayer.PlaybackSpeed = settings.SongEditorSettings.MusicPlaybackSpeed;
        Bind(musicPlaybackSpeedSlider,
            () => settings.SongEditorSettings.MusicPlaybackSpeed,
            newValue => SetMusicPlaybackSpeed(newValue),
            false);
        resetMusicPlaybackSpeedButton.RegisterCallbackButtonTriggered(() =>
        {
            SetMusicPlaybackSpeed(1);
            musicPlaybackSpeedSlider.value = 1;
        });

        // Recording settings
        recordingSourceItemPickerControl = new LabeledItemPickerControl<ESongEditorRecordingSource>(recordingSourceItemPicker, EnumUtils.GetValuesAsList<ESongEditorRecordingSource>());
        recordingSourceItemPickerControl.Bind(
            () => settings.SongEditorSettings.RecordingSource,
            newValue => settings.SongEditorSettings.RecordingSource = newValue);

        // Mic recording settings
        List<MicProfile> micProfiles = settings.MicProfiles;
        List<MicProfile> enabledAndConnectedMicProfiles = micProfiles.Where(it => it.IsEnabledAndConnected).ToList();
        micDeviceItemPickerControl = new LabeledItemPickerControl<MicProfile>(micDeviceItemPicker, enabledAndConnectedMicProfiles);
        micDeviceItemPickerControl.GetLabelTextFunction = micProfile => micProfile != null ? micProfile.Name : "";
        if (settings.SongEditorSettings.MicProfile == null
            || !settings.SongEditorSettings.MicProfile.IsEnabledAndConnected)
        {
            settings.SongEditorSettings.MicProfile = enabledAndConnectedMicProfiles.FirstOrDefault();
        }
        micDeviceItemPickerControl.Bind(
            () => settings.SongEditorSettings.MicProfile,
            newValue => settings.SongEditorSettings.MicProfile = newValue);

        Bind(micOctaveOffsetTextField,
            () => settings.SongEditorSettings.MicOctaveOffset.ToString(),
            newValue => PropertyUtils.TrySetIntFromString(newValue, newIntValue => settings.SongEditorSettings.MicOctaveOffset = newIntValue));
        Bind(micDelayTextField,
            () => settings.SongEditorSettings.MicDelayInMillis.ToString(),
            newValue => PropertyUtils.TrySetIntFromString(newValue, newIntValue => settings.SongEditorSettings.MicDelayInMillis = newIntValue));

        // Button recording settings
        Bind(buttonRecordingPitchTextField,
            () => MidiUtils.GetAbsoluteName(settings.SongEditorSettings.MidiNoteForButtonRecording),
            newValue =>
            {
                if (MidiUtils.TryParseMidiNoteName(newValue, out int newMidiNote))
                {
                    settings.SongEditorSettings.MidiNoteForButtonRecording = newMidiNote;
                }
            });
        Bind(buttonRecordingButtonTextField,
            () => settings.SongEditorSettings.ButtonDisplayNameForButtonRecording,
            newValue => settings.SongEditorSettings.ButtonDisplayNameForButtonRecording = newValue);

        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.RecordingSource)
            .Subscribe(_ => UpdateRecordingSettingsVisibility())
            .AddTo(gameObject);

        // MIDI settings
        Bind(midiNotePlayAlongToggle,
            () => settings.SongEditorSettings.MidiSoundPlayAlongEnabled,
            newValue => settings.SongEditorSettings.MidiSoundPlayAlongEnabled = newValue);
        Bind(midiGainSlider,
            () => settings.SongEditorSettings.MidiGain,
            newValue => settings.SongEditorSettings.MidiGain = newValue);
        Bind(midiVelocitySlider,
            () => settings.SongEditorSettings.MidiVelocity,
            newValue => settings.SongEditorSettings.MidiVelocity = (int)newValue);
        Bind(midiDelayTextField,
            () => settings.SongEditorSettings.MidiPlaybackOffsetInMillis.ToString(),
            newValue => PropertyUtils.TrySetIntFromString(newValue, newIntValue => settings.SongEditorSettings.MidiPlaybackOffsetInMillis = newIntValue));

        importMidiFileButton.RegisterCallbackButtonTriggered(() => CreateImportMidiFileDialog());

        // Show / hide VisualElements
        Bind(showLyricsAreaToggle,
            () => settings.SongEditorSettings.ShowLyricsArea,
            newValue => settings.SongEditorSettings.ShowLyricsArea = newValue);
        Bind(showStatusBarToggle,
            () => settings.SongEditorSettings.ShowStatusBar,
            newValue => settings.SongEditorSettings.ShowStatusBar = newValue);
        Bind(showControlHintsToggle,
            () => settings.SongEditorSettings.ShowControlHints,
            newValue => settings.SongEditorSettings.ShowControlHints = newValue);
        Bind(showVideoAreaToggle,
            () => settings.SongEditorSettings.ShowVideoArea,
            newValue => settings.SongEditorSettings.ShowVideoArea = newValue);
        Bind(showVirtualPianoToggle,
            () => settings.SongEditorSettings.ShowVirtualPianoArea,
            newValue => settings.SongEditorSettings.ShowVirtualPianoArea = newValue);

        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.ShowLyricsArea)
            .Subscribe(newValue => lyricsArea.SetVisibleByDisplay(newValue))
            .AddTo(gameObject);
        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.ShowStatusBar)
            .Subscribe(newValue => statusBar.SetVisibleByDisplay(newValue))
            .AddTo(gameObject);
        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.ShowVideoArea)
            .Subscribe(newValue => videoArea.SetVisibleByDisplay(newValue))
            .AddTo(gameObject);
        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.ShowVirtualPianoArea)
            .Subscribe(newValue => virtualPiano.SetVisibleByDisplay(newValue))
            .AddTo(gameObject);

        // Grid size
        Bind(gridSizeTextField,
            () => settings.SongEditorSettings.GridSizeInDevicePixels.ToString(),
            newValue => PropertyUtils.TrySetIntFromString(newValue, newIntValue => settings.SongEditorSettings.GridSizeInDevicePixels = newIntValue));
        Bind(sentenceLineSizeTextField,
            () => settings.SongEditorSettings.SentenceLineSizeInDevicePixels.ToString(),
            newValue => PropertyUtils.TrySetIntFromString(newValue, newIntValue => settings.SongEditorSettings.SentenceLineSizeInDevicePixels = newIntValue));
    }

    private void CreateImportMidiFileDialog()
    {
        songEditorSceneControl.CreatePathInputDialog("Import MIDI File",
            "Enter the absolute path to the MIDI file.",
            settings.SongEditorSettings.LastMidiFilePath,
            path =>
            {
                settings.SongEditorSettings.LastMidiFilePath = path;
                midiFileImporter.ImportMidiFile(path);
            });
    }

    private void UpdateRecordingSettingsVisibility()
    {
        bool micRecordingSettingsVisible = settings.SongEditorSettings.RecordingSource == ESongEditorRecordingSource.Microphone;
        micRecordingContainer.SetVisibleByDisplay(micRecordingSettingsVisible);

        bool buttonRecordingSettingsVisible = settings.SongEditorSettings.RecordingSource == ESongEditorRecordingSource.KeyboardButton;
        buttonRecordingContainer.SetVisibleByDisplay(buttonRecordingSettingsVisible);
    }

    private void SetMusicPlaybackSpeed(float newValue)
    {
        float newValueRounded = (float)Math.Round(newValue, 1);
        if (Mathf.Abs(newValueRounded - 1) < 0.1)
        {
            // Round to exactly 1 to eliminate manipulation of playback speed. Otherwise there will be noise in the audio.
            newValueRounded = 1;
        }

        settings.SongEditorSettings.MusicPlaybackSpeed = newValueRounded;
        songAudioPlayer.PlaybackSpeed = newValueRounded;
    }

    private void Bind<T>(BaseField<T> baseField, Func<T> valueGetter, Action<T> valueSetter, bool observeValueGetter = true)
    {
        baseField.value = valueGetter();
        baseField.RegisterValueChangedCallback(evt => valueSetter(evt.newValue));

        // Update field when settings change.
        if (observeValueGetter)
        {
            this.ObserveEveryValueChanged(_ => valueGetter())
                .Where(newValue => !object.Equals(baseField.value, newValue))
                .Subscribe(newValue => baseField.value = newValue)
                .AddTo(gameObject);
        }
    }
}
