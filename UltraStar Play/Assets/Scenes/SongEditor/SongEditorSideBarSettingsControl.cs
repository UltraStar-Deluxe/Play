using System;
using System.Collections.Generic;
using System.Globalization;
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
    
    [Inject(UxmlName = R.UxmlNames.selectModelPathButton)]
    private Button selectModelPathButton;

    [Inject(UxmlName = R.UxmlNames.micDeviceItemPicker)]
    private ItemPicker micDeviceItemPicker;

    [Inject(UxmlName = R.UxmlNames.micDelayTextField)]
    private TextField micDelayTextField;

    [Inject(UxmlName = R.UxmlNames.speechRecognitionWhenRecordingToggle)]
    private Toggle speechRecognitionWhenRecordingToggle;

    [Inject(UxmlName = R.UxmlNames.buttonRecordingPitchTextField)]
    private TextField buttonRecordingPitchTextField;

    [Inject(UxmlName = R.UxmlNames.micRecordingPitchTextField)]
    private TextField micRecordingPitchTextField;
    
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

    [Inject(UxmlName = R.UxmlNames.showNotePitchLabelToggle)]
    private Toggle showNotePitchLabelToggle;

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

    [Inject(UxmlName = R.UxmlNames.importMidiFileButton)]
    private Button importMidiFileButton;
    
    [Inject(UxmlName = R.UxmlNames.speechRecognitionModelPathTextField)]
    private TextField speechRecognitionModelPathTextField;

    [Inject(UxmlName = R.UxmlNames.speechRecognitionPhrasesTextField)]
    private TextField speechRecognitionPhrasesTextField;

    [Inject(UxmlName = R.UxmlNames.pitchDetectionAlgorithmItemPicker)]
    private ItemPicker pitchDetectionAlgorithmItemPicker;

    [Inject(UxmlName = R.UxmlNames.audioSeparationCommandTextField)]
    private TextField audioSeparationCommandTextField;

    [Inject(UxmlName = R.UxmlNames.audioSeparationButton)]
    private Button audioSeparationButton;

    [Inject(UxmlName = R.UxmlNames.playbackAudioPicker)]
    private ItemPicker playbackAudioPicker;

    [Inject(UxmlName = R.UxmlNames.speechRecognitionAudioPicker)]
    private ItemPicker speechRecognitionAudioPicker;

    [Inject(UxmlName = R.UxmlNames.pitchDetectionAudioPicker)]
    private ItemPicker pitchDetectionAudioPicker;

    [Inject(UxmlName = R.UxmlNames.timeLabelFormatPicker)]
    private ItemPicker timeLabelFormatPicker;
    
    [Inject(UxmlName = R.UxmlNames.pitchLabelFormatPicker)]
    private ItemPicker pitchLabelFormatPicker;
    
    [Inject]
    private SongMeta songMeta;

    [Inject]
    private Settings settings;

    [Inject]
    private GameObject gameObject;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private ServerSideConnectRequestManager serverSideConnectRequestManager;

    [Inject]
    private AudioSeparationManager audioSeparationManager;

    [Inject]
    private Injector injector;

    private LabeledItemPickerControl<ESongEditorRecordingSource> recordingSourceItemPickerControl;
    private LabeledItemPickerControl<MicProfile> micDeviceItemPickerControl;
    private LabeledItemPickerControl<ESongEditorSamplesSource> playbackAudioItemPickerControl;
    private LabeledItemPickerControl<ESongEditorSamplesSource> speechRecognitionAudioItemPickerControl;
    private LabeledItemPickerControl<ESongEditorSamplesSource> pitchDetectionAudioItemPickerControl;
    private LabeledItemPickerControl<ERecordNotesOrAudio> recordNotesOrAudioItemPickerControl;

    private readonly ImportMidiFileDialogControl importMidiFileDialogControl = new();
    
    public void OnInjectionFinished()
    {
        injector.Inject(importMidiFileDialogControl);

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
        resetMusicPlaybackSpeedButton.RegisterCallbackButtonTriggered(_ =>
        {
            SetMusicPlaybackSpeed(1);
            musicPlaybackSpeedSlider.value = 1;
        });

        playbackAudioItemPickerControl = new(playbackAudioPicker, EnumUtils.GetValuesAsList<ESongEditorSamplesSource>());
        playbackAudioItemPickerControl.Bind(
            () => settings.SongEditorSettings.PlaybackSamplesSource,
            newValue => settings.SongEditorSettings.PlaybackSamplesSource = newValue);

        // Mic recording settings
        List<MicProfile> micProfiles = settings.MicProfiles;
        List<MicProfile> enabledAndConnectedMicProfiles = micProfiles
            .Where(it => it.IsEnabledAndConnected(serverSideConnectRequestManager))
            .ToList();
        micDeviceItemPickerControl = new(micDeviceItemPicker, enabledAndConnectedMicProfiles);
        micDeviceItemPickerControl.GetLabelTextFunction = micProfile => micProfile != null ? micProfile.Name : "";
        if (settings.SongEditorSettings.MicProfile == null
            || !settings.SongEditorSettings.MicProfile.IsEnabledAndConnected(serverSideConnectRequestManager))
        {
            settings.SongEditorSettings.MicProfile = enabledAndConnectedMicProfiles.FirstOrDefault();
        }
        micDeviceItemPickerControl.Bind(
            () => settings.SongEditorSettings.MicProfile,
            newValue => settings.SongEditorSettings.MicProfile = newValue);
        new AutoFitLabelControl(micDeviceItemPickerControl.ItemPicker.ItemLabel, 8, 15);
        
        Bind(micRecordingPitchTextField,
            () => MidiUtils.GetAbsoluteName(settings.SongEditorSettings.DefaultPitchForCreatedNotes),
            newValue =>
            {
                if (MidiUtils.TryParseMidiNoteName(newValue, out int newMidiNote))
                {
                    settings.SongEditorSettings.DefaultPitchForCreatedNotes = newMidiNote;
                }
            });
        
        Bind(micDelayTextField,
            () => settings.SongEditorSettings.MicDelayInMillis.ToString(),
            newValue => PropertyUtils.TrySetIntFromString(newValue, newIntValue => settings.SongEditorSettings.MicDelayInMillis = newIntValue));

        // Record notes or audio
        Bind(speechRecognitionWhenRecordingToggle,
            () => settings.SongEditorSettings.speechRecognitionWhenRecording,
            newValue => settings.SongEditorSettings.speechRecognitionWhenRecording = newValue);

        // Button recording settings
        Bind(buttonRecordingPitchTextField,
            () => MidiUtils.GetAbsoluteName(settings.SongEditorSettings.DefaultPitchForCreatedNotes),
            newValue =>
            {
                if (MidiUtils.TryParseMidiNoteName(newValue, out int newMidiNote))
                {
                    settings.SongEditorSettings.DefaultPitchForCreatedNotes = newMidiNote;
                }
            });
        Bind(buttonRecordingButtonTextField,
            () => settings.SongEditorSettings.ButtonDisplayNameForButtonRecording,
            newValue => settings.SongEditorSettings.ButtonDisplayNameForButtonRecording = newValue);

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

        importMidiFileButton.RegisterCallbackButtonTriggered(_ => importMidiFileDialogControl.OpenDialog());

        // Speech recognition
        Bind(speechRecognitionModelPathTextField,
            () => settings.SongEditorSettings.SpeechRecognitionModelPath,
            newValue => settings.SongEditorSettings.SpeechRecognitionModelPath = newValue);
        Bind(speechRecognitionPhrasesTextField,
            () => settings.SongEditorSettings.SpeechRecognitionPhrases,
            newValue => settings.SongEditorSettings.SpeechRecognitionPhrases = newValue);
        
        if (PlatformUtils.IsStandalone)
        {
            selectModelPathButton.RegisterCallbackButtonTriggered(_ =>
            {
                string selectedFolder = FileSystemDialogUtils.OpenFolderDialog("Select Speech Recognition Model", speechRecognitionModelPathTextField.value);
                if (selectedFolder.IsNullOrEmpty())
                {
                    return;
                }

                speechRecognitionModelPathTextField.value = selectedFolder;
            });
        }
        else
        {
            selectModelPathButton.HideByDisplay();
        }

        speechRecognitionAudioItemPickerControl = new(speechRecognitionAudioPicker, EnumUtils.GetValuesAsList<ESongEditorSamplesSource>());
        speechRecognitionAudioItemPickerControl.Bind(
            () => settings.SongEditorSettings.SpeechRecognitionSamplesSource,
            newValue => settings.SongEditorSettings.SpeechRecognitionSamplesSource = newValue);

        // Pitch detection
        new PitchDetectionAlgorithmPickerControl(pitchDetectionAlgorithmItemPicker)
            .Bind(() => settings.SongEditorSettings.PitchDetectionAlgorithm,
                newValue => settings.SongEditorSettings.PitchDetectionAlgorithm = newValue);
        new AutoFitLabelControl(pitchDetectionAlgorithmItemPicker.ItemLabel, 8, 15);
        
        pitchDetectionAudioItemPickerControl = new(pitchDetectionAudioPicker, EnumUtils.GetValuesAsList<ESongEditorSamplesSource>());
        pitchDetectionAudioItemPickerControl.Bind(
            () => settings.SongEditorSettings.PitchDetectionSamplesSource,
            newValue => settings.SongEditorSettings.PitchDetectionSamplesSource = newValue);

        // Audio separation (Spleeter)
        Bind(audioSeparationCommandTextField,
            () => settings.SongEditorSettings.AudioSeparationCommand,
            newValue => settings.SongEditorSettings.AudioSeparationCommand = newValue);
        audioSeparationButton.RegisterCallbackButtonTriggered(_ =>
        {
            if (SongMetaUtils.VocalsAudioResourceExists(songMeta)
                && SongMetaUtils.InstrumentalAudioResourceExists(songMeta))
            {
                UiManager.CreateNotification("Vocals and instrumental audio already exists");
                return;
            }
            audioSeparationManager.ProcessSongMeta(songMeta);
            audioSeparationButton.SetEnabled(false);
        });
        if (SongMetaUtils.VocalsAudioResourceExists(songMeta)
            && SongMetaUtils.InstrumentalAudioResourceExists(songMeta))
        {
            audioSeparationButton.SetEnabled(false);
        }

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
        Bind(showNotePitchLabelToggle,
            () => settings.SongEditorSettings.ShowNotePitchLabel,
            newValue => settings.SongEditorSettings.ShowNotePitchLabel = newValue);

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
            () => settings.SongEditorSettings.GridSizeInPx.ToString(CultureInfo.InvariantCulture),
            newValue => PropertyUtils.TrySetFloatFromString(newValue, newFloatValue => settings.SongEditorSettings.GridSizeInPx = newFloatValue));
        Bind(sentenceLineSizeTextField,
            () => settings.SongEditorSettings.SentenceLineSizeInPx.ToString(CultureInfo.InvariantCulture),
            newValue => PropertyUtils.TrySetFloatFromString(newValue, newFloatValue => settings.SongEditorSettings.SentenceLineSizeInPx = newFloatValue));
        
        // Labels
        new LabeledItemPickerControl<ESongEditorTimeLabelFormat>(timeLabelFormatPicker, EnumUtils.GetValuesAsList<ESongEditorTimeLabelFormat>())
            .Bind(() => settings.SongEditorSettings.TimeLabelFormat,
                newValue => settings.SongEditorSettings.TimeLabelFormat = newValue);
        
        new LabeledItemPickerControl<ESongEditorPitchLabelFormat>(pitchLabelFormatPicker, EnumUtils.GetValuesAsList<ESongEditorPitchLabelFormat>())
            .Bind(() => settings.SongEditorSettings.PitchLabelFormat,
                newValue => settings.SongEditorSettings.PitchLabelFormat = newValue);
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
        FieldBindingUtils.Bind(gameObject, baseField, valueGetter, valueSetter, observeValueGetter);
    }

    public enum ERecordNotesOrAudio
    {
        RecordNotes,
        RecordAudio,
    }
}
