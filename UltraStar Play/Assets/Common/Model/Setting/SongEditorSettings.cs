using System;

[Serializable]
public class SongEditorSettings
{
    // Recording in SongEditorScene
    public ESongEditorRecordingSource RecordingSource { get; set; }
    public int MicOctaveOffset { get; set; }
    public int MicDelayInMillis { get; set; } = 450;
    public int MidiNoteForButtonRecording { get; set; } = MidiUtils.MidiNoteConcertPitch;
}