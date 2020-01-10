using System;
using System.Collections.Generic;

[Serializable]
public class SongEditorSettings
{
    // Recording in SongEditorScene
    public ESongEditorRecordingSource RecordingSource { get; set; }
    public int MicOctaveOffset { get; set; }
    public int MicDelayInMillis { get; set; } = 450;
    public int MidiNoteForButtonRecording { get; set; } = MidiUtils.MidiNoteConcertPitch;

    public bool AdjustFollowingNotes { get; set; }

    // Option to show / hide voices.
    // Contains the names of the voices that should be hidden.
    public List<string> HideVoices { get; private set; } = new List<string>();

    public bool SaveCopyOfOriginalFile { get; set; } = true;
}