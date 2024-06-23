using System;

[Serializable]
public class SongEditorSettings
{
    public bool AutoSave { get; set; }

    public int MusicVolumePercent { get; set; } = 100;
    public ESongEditorSamplesSource PlaybackSamplesSource { get; set; } = ESongEditorSamplesSource.OriginalMusic;
    public ESongEditorAudioWaveformSamplesSource AudioWaveformSamplesSource { get; set; } = ESongEditorAudioWaveformSamplesSource.SameAsPlayback;
    public bool GoToLastPlaybackPosition { get; set; } = true;
    public int PlaybackPreBeginInMillis { get; set; }
    public int PlaybackPostEndInMillis { get; set; }

    public ESongEditorDrawNoteLayer DrawNoteLayer { get; set; }

    // Recording in SongEditorScene
    public MicProfile MicProfile { get; set; }
    public int MicDelayInMillis { get; set; } = 450;
    public int DefaultPitchForCreatedNotes { get; set; } = MidiUtils.MidiNoteConcertPitch;
    public string ButtonDisplayNameForButtonRecording { get; set; } = "N";
    public string ButtonRecordingLyrics { get; set; } = "";
    public bool SpeechRecognitionWhenRecording { get; set; } = true;
    public string SpeechRecognitionLanguage { get; set; } = EWhisperLanguage.English.ToString().ToLowerInvariant();
    public string SpeechRecognitionPrompt { get; set; } = "";

    public bool AdjustFollowingNotes { get; set; }

    // Velocity should be between 0 and 127
    public int MidiVelocity { get; set; } = 100;
    // Gain is similar to volume and should be between 0 and 1 to make it more silent and above 1 to make it louder.
    public float MidiGain { get; set; } = 1;
    public bool MidiSoundPlayAlongEnabled { get; set; }
    public int MidiPlaybackOffsetInMillis { get; set; }
    public string LastMidiFilePath { get; set; } = "";

    public bool ShowRightSideBar { get; set; } = true;
    public bool ShowAudioWaveformInBackground { get; set; } = true;
    public bool ShowVideoArea { get; set; } = true;
    public bool ShowStatusBar { get; set; } = true;
    public bool ShowVirtualPianoArea { get; set; }
    public bool SmallLeftSideBar { get; set; }
    public bool ShowControlHints { get; set; } = true;
    public bool ShowNotePitchLabel { get; set; } = true;
    public ESongEditorTimeLabelFormat TimeLabelFormat { get; set; } = ESongEditorTimeLabelFormat.Beats;
    public ESongEditorPitchLabelFormat PitchLabelFormat { get; set; } = ESongEditorPitchLabelFormat.Notes;

    public float GridSizeInPx { get; set; } = 1;
    public float SentenceLineSizeInPx { get; set; } = 2;

    // Speech recognition
    public string SpeechRecognitionModelPath { get; set; } = "";
    public string SpeechRecognitionPhrases { get; set; } = "";
    public ESongEditorSamplesSource SpeechRecognitionSamplesSource { get; set; } = ESongEditorSamplesSource.Vocals;
    public bool SplitSyllablesAfterSpeechRecognition { get; set; } = true;

    // Pitch detection
    public EPitchDetectionAlgorithm PitchDetectionAlgorithm { get; set; } = EPitchDetectionAlgorithm.Dywa;
    public ESongEditorSamplesSource PitchDetectionSamplesSource { get; set; } = ESongEditorSamplesSource.Vocals;
    public string BasicPitchCommand { get; set; } = "";

    // Audio separation
    public string AudioSeparationCommand { get; set; } = "";

    // Editing
    public int SpaceBetweenNotesInMillis { get; set; } = SpaceBetweenNotesUtils.DefaultSpaceBetweenNotesInMillis;
}
