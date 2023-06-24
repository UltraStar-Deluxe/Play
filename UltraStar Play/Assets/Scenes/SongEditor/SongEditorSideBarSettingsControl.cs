using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

    [Inject(UxmlName = R.UxmlNames.spaceBetweenNotesTimeInMillisTextField)]
    private IntegerField spaceBetweenNotesTimeInMillisTextField;
    
    [Inject(UxmlName = R.UxmlNames.addSpaceBetweenNotesButton)]
    private Button addSpaceBetweenNotesButton;
    
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
    
    [Inject(UxmlName = R.UxmlNames.speechRecognitionLanguageChooser)]
    private EnumField speechRecognitionLanguageChooser;

    [Inject(UxmlName = R.UxmlNames.speechRecognitionPromptTextField)]
    private TextField speechRecognitionPromptTextField;
    
    [Inject(UxmlName = R.UxmlNames.micDeviceItemPicker)]
    private ItemPicker micDeviceItemPicker;

    [Inject(UxmlName = R.UxmlNames.drawNoteLayerPicker)]
    private ItemPicker drawNoteLayerPicker;
    
    [Inject(UxmlName = R.UxmlNames.micDelayTextField)]
    private TextField micDelayTextField;

    [Inject(UxmlName = R.UxmlNames.speechRecognitionWhenRecordingToggle)]
    private Toggle speechRecognitionWhenRecordingToggle;

    [Inject(UxmlName = R.UxmlNames.buttonRecordingLyricsTextField)]
    private TextField buttonRecordingLyricsTextField;
    
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

    [Inject(UxmlName = R.UxmlNames.showRightSideBarToggle)]
    private Toggle showRightSideBarToggle;
    
    [Inject(UxmlName = R.UxmlNames.showAudioWaveformInBackgroundToggle)]
    private Toggle showAudioWaveformInBackgroundToggle;

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

    [Inject(UxmlName = R.UxmlNames.rightSideBar)]
    private VisualElement rightSideBar;

    [Inject(UxmlName = R.UxmlNames.importMidiFileButton)]
    private Button importMidiFileButton;
    
    [Inject(UxmlName = R.UxmlNames.speechRecognitionModelPathTextField)]
    private TextField speechRecognitionModelPathTextField;

    [Inject(UxmlName = R.UxmlNames.speechRecognitionPhrasesTextField)]
    private TextField speechRecognitionPhrasesTextField;

    [Inject(UxmlName = R.UxmlNames.pitchDetectionAlgorithmItemPicker)]
    private ItemPicker pitchDetectionAlgorithmItemPicker;

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
    
    [Inject(UxmlName = R.UxmlNames.settingsSideBarContainer)]
    private VisualElement settingsSideBarContainer;
    
    [Inject]
    private SongMeta songMeta;

    [Inject]
    private Settings settings;
    
    [Inject]
    private NonPersistentSettings nonPersistentSettings;

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
    
    [Inject]
    private SongEditorSelectionControl selectionControl;
    
    [Inject]
    private SpaceBetweenNotesAction spaceBetweenNotesAction;

    private LabeledItemPickerControl<ESongEditorRecordingSource> recordingSourceItemPickerControl;
    private LabeledItemPickerControl<MicProfile> micDeviceItemPickerControl;
    private LabeledItemPickerControl<ESongEditorSamplesSource> playbackAudioItemPickerControl;
    private LabeledItemPickerControl<ESongEditorSamplesSource> speechRecognitionAudioItemPickerControl;
    private LabeledItemPickerControl<ESongEditorSamplesSource> pitchDetectionAudioItemPickerControl;
    private LabeledItemPickerControl<ERecordNotesOrAudio> recordNotesOrAudioItemPickerControl;
    private LabeledItemPickerControl<ESongEditorDrawNoteLayer> drawNoteLayerPickerControl;

    private readonly ImportMidiFileDialogControl importMidiFileDialogControl = new();
    
    public void OnInjectionFinished()
    {
        injector.Inject(importMidiFileDialogControl);

        // Fold all AccordionItems
        settingsSideBarContainer.Query<AccordionItem>().ForEach(it => it.HideAccordionContent());
        
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
            () => settings.SongEditorSettings.MusicVolumePercent,
            newValue => settings.SongEditorSettings.MusicVolumePercent = (int) newValue);

        drawNoteLayerPickerControl = new(drawNoteLayerPicker, EnumUtils.GetValuesAsList<ESongEditorDrawNoteLayer>());
        drawNoteLayerPickerControl.GetLabelTextFunction = item => StringUtils.ToTitleCase(ObjectUtils.NullableToString(item, ""));
        drawNoteLayerPickerControl.Bind(
            () => settings.SongEditorSettings.DrawNoteLayer,
            newValue => settings.SongEditorSettings.DrawNoteLayer = newValue);
        
        // Add space between notes
        Bind(spaceBetweenNotesTimeInMillisTextField,
            () => settings.SongEditorSettings.SpaceBetweenNotesInMillis,
            newValue => settings.SongEditorSettings.SpaceBetweenNotesInMillis = newValue);

        addSpaceBetweenNotesButton.RegisterCallbackButtonTriggered(_ => AddSpaceBetweenNotes());
        
        // Playback speed
        songAudioPlayer.PlaybackSpeed = nonPersistentSettings.SongEditorMusicPlaybackSpeed.Value;
        Bind(musicPlaybackSpeedSlider,
            () => nonPersistentSettings.SongEditorMusicPlaybackSpeed.Value,
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
        micDeviceItemPickerControl.GetLabelTextFunction = micProfile => micProfile != null ? micProfile.GetDisplayNameWithChannel() : "";
        if (settings.SongEditorSettings.MicProfile == null
            || !settings.SongEditorSettings.MicProfile.IsEnabledAndConnected(serverSideConnectRequestManager))
        {
            settings.SongEditorSettings.MicProfile = enabledAndConnectedMicProfiles.FirstOrDefault();
        }
        micDeviceItemPickerControl.Bind(
            () => settings.SongEditorSettings.MicProfile,
            newValue => settings.SongEditorSettings.MicProfile = newValue);
        new AutoFitLabelControl(micDeviceItemPickerControl.ItemPicker.ItemLabel, 8, 15);
        
        micRecordingPitchTextField.DisableParseEscapeSequences();
        Bind(micRecordingPitchTextField,
            () => MidiUtils.GetAbsoluteName(settings.SongEditorSettings.DefaultPitchForCreatedNotes),
            newValue =>
            {
                if (MidiUtils.TryParseMidiNoteName(newValue, out int newMidiNote))
                {
                    settings.SongEditorSettings.DefaultPitchForCreatedNotes = newMidiNote;
                }
            });

        buttonRecordingLyricsTextField.DisableParseEscapeSequences();
        buttonRecordingLyricsTextField.selectAllOnFocus = false;
        buttonRecordingLyricsTextField.selectAllOnMouseUp = false;
        Bind(buttonRecordingLyricsTextField,
            () => settings.SongEditorSettings.ButtonRecordingLyrics,
            newValue => settings.SongEditorSettings.ButtonRecordingLyrics = newValue);
        
        micDelayTextField.DisableParseEscapeSequences();
        Bind(micDelayTextField,
            () => settings.SongEditorSettings.MicDelayInMillis.ToString(),
            newValue => PropertyUtils.TrySetIntFromString(newValue, newIntValue => settings.SongEditorSettings.MicDelayInMillis = newIntValue));

        // Record notes or audio
        Bind(speechRecognitionWhenRecordingToggle,
            () => settings.SongEditorSettings.SpeechRecognitionWhenRecording,
            newValue => settings.SongEditorSettings.SpeechRecognitionWhenRecording = newValue);

        // Button recording settings
        buttonRecordingPitchTextField.DisableParseEscapeSequences();
        Bind(buttonRecordingPitchTextField,
            () => MidiUtils.GetAbsoluteName(settings.SongEditorSettings.DefaultPitchForCreatedNotes),
            newValue =>
            {
                if (MidiUtils.TryParseMidiNoteName(newValue, out int newMidiNote))
                {
                    settings.SongEditorSettings.DefaultPitchForCreatedNotes = newMidiNote;
                }
            });
        
        buttonRecordingButtonTextField.DisableParseEscapeSequences();
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
        
        midiDelayTextField.DisableParseEscapeSequences();
        Bind(midiDelayTextField,
            () => settings.SongEditorSettings.MidiPlaybackOffsetInMillis.ToString(),
            newValue => PropertyUtils.TrySetIntFromString(newValue, newIntValue => settings.SongEditorSettings.MidiPlaybackOffsetInMillis = newIntValue));

        importMidiFileButton.RegisterCallbackButtonTriggered(_ => importMidiFileDialogControl.OpenDialog());

        // Speech recognition
        Bind(speechRecognitionLanguageChooser,
            () =>
            {
                if (Enum.TryParse(settings.SongEditorSettings.SpeechRecognitionLanguage, out EWhisperLanguage whisperLanguage))
                {
                    return whisperLanguage;
                }
                return EWhisperLanguage.English;
            },
            newValue => settings.SongEditorSettings.SpeechRecognitionLanguage = newValue.ToString());
        
        Bind(speechRecognitionPromptTextField,
            () => settings.SongEditorSettings.SpeechRecognitionPrompt,
            newValue => settings.SongEditorSettings.SpeechRecognitionPrompt = newValue);
        
        sentenceLineSizeTextField.DisableParseEscapeSequences();
        Bind(speechRecognitionModelPathTextField,
            () => settings.SongEditorSettings.SpeechRecognitionModelPath,
            newValue => settings.SongEditorSettings.SpeechRecognitionModelPath = newValue);
        
        speechRecognitionPhrasesTextField.DisableParseEscapeSequences();
        Bind(speechRecognitionPhrasesTextField,
            () => settings.SongEditorSettings.SpeechRecognitionPhrases,
            newValue => settings.SongEditorSettings.SpeechRecognitionPhrases = newValue);
        
        if (PlatformUtils.IsStandalone)
        {
            selectModelPathButton.RegisterCallbackButtonTriggered(_ =>
            {
                string oldFolder = FileUtils.Exists(speechRecognitionModelPathTextField.value)
                    ? new FileInfo(speechRecognitionModelPathTextField.value).DirectoryName
                    : "";
                string selectedFile = FileSystemDialogUtils.OpenFileDialog(
                    "Select Speech Recognition Model",
                    oldFolder,
                    FileSystemDialogUtils.CreateExtensionFilters("Model files", "bin"));
                if (selectedFile.IsNullOrEmpty()
                    || !FileUtils.Exists(selectedFile))
                {
                    return;
                }

                speechRecognitionModelPathTextField.value = selectedFile;
            });
        }
        else
        {
            selectModelPathButton.HideByDisplay();
        }

        List<ESongEditorSamplesSource> speechAndPitchAnalysisSampleSources = new List<ESongEditorSamplesSource>
        {
            ESongEditorSamplesSource.OriginalMusic,
            ESongEditorSamplesSource.Vocals,
            ESongEditorSamplesSource.Recording,
        };

        speechRecognitionAudioItemPickerControl = new(speechRecognitionAudioPicker, speechAndPitchAnalysisSampleSources);
        speechRecognitionAudioItemPickerControl.Bind(
            () => settings.SongEditorSettings.SpeechRecognitionSamplesSource,
            newValue => settings.SongEditorSettings.SpeechRecognitionSamplesSource = newValue);

        // Pitch detection
        new PitchDetectionAlgorithmPickerControl(pitchDetectionAlgorithmItemPicker)
            .Bind(() => settings.SongEditorSettings.PitchDetectionAlgorithm,
                newValue => settings.SongEditorSettings.PitchDetectionAlgorithm = newValue);
        new AutoFitLabelControl(pitchDetectionAlgorithmItemPicker.ItemLabel, 8, 15);
        
        pitchDetectionAudioItemPickerControl = new(pitchDetectionAudioPicker, speechAndPitchAnalysisSampleSources);
        pitchDetectionAudioItemPickerControl.Bind(
            () => settings.SongEditorSettings.PitchDetectionSamplesSource,
            newValue => settings.SongEditorSettings.PitchDetectionSamplesSource = newValue);
        
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
        Bind(showRightSideBarToggle,
            () => settings.SongEditorSettings.ShowRightSideBar,
            newValue => settings.SongEditorSettings.ShowRightSideBar = newValue);
        Bind(showAudioWaveformInBackgroundToggle,
            () => settings.SongEditorSettings.ShowAudioWaveformInBackground,
            newValue => settings.SongEditorSettings.ShowAudioWaveformInBackground = newValue);
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

        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.ShowRightSideBar)
            .Subscribe(newValue => rightSideBar.SetVisibleByDisplay(newValue))
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
        gridSizeTextField.DisableParseEscapeSequences();
        Bind(gridSizeTextField,
            () => settings.SongEditorSettings.GridSizeInPx.ToString(CultureInfo.InvariantCulture),
            newValue => PropertyUtils.TrySetFloatFromString(newValue, newFloatValue => settings.SongEditorSettings.GridSizeInPx = newFloatValue));
        
        sentenceLineSizeTextField.DisableParseEscapeSequences();
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

    private void AddSpaceBetweenNotes()
    {
        int spaceInMillis = settings.SongEditorSettings.SpaceBetweenNotesInMillis;
        if (spaceInMillis <= 0)
        {
            return;
        }
        
        List<Note> selectedNotes = selectionControl.GetSelectedNotes();
        if (selectedNotes.IsNullOrEmpty())
        {
            // Perform on all notes, but per voice
            songMeta.GetVoices()
                .ForEach(voice => spaceBetweenNotesAction.ExecuteAndNotify(songMeta, SongMetaUtils.GetAllNotes(voice), spaceInMillis));
        }
        else
        {
            spaceBetweenNotesAction.ExecuteAndNotify(songMeta, selectedNotes, spaceInMillis);
        }
    }

    private void SetMusicPlaybackSpeed(float newValue)
    {
        float newValueRounded = (float)Math.Round(newValue, 1);
        if (Mathf.Abs(newValueRounded - 1) < 0.1)
        {
            // Round to exactly 1 to eliminate manipulation of playback speed. Otherwise there will be noise in the audio.
            newValueRounded = 1;
        }

        nonPersistentSettings.SongEditorMusicPlaybackSpeed.Value = newValueRounded;
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
