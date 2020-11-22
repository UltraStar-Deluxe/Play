using System;
using System.Collections.Generic;

[Serializable]
public class SongEditorSettings
{
    public float MusicVolume { get; set; } = 1;
    public float MusicPlaybackSpeed { get; set; } = 1;

    // Recording in SongEditorScene
    public ESongEditorRecordingSource RecordingSource { get; set; }
    public int MicOctaveOffset { get; set; }
    public int MicDelayInMillis { get; set; } = 450;
    public int MidiNoteForButtonRecording { get; set; } = MidiUtils.MidiNoteConcertPitch;

    public bool AdjustFollowingNotes { get; set; }

    // Velocity should be between 0 and 127
    public int MidiVelocity { get; set; } = 100;
    // Gain is similar to volume and should be between 0 and 1 to make it more silent and above 1 to make it louder.
    public float MidiGain { get; set; } = 1;
    public bool MidiSoundPlayAlongEnabled { get; set; } = true;
    public int MidiPlaybackOffsetInMillis { get; set; }
    public string MidiFilePath { get; set; } = "";

    // Option to show / hide voices.
    // Contains the names of the voices that should be hidden.
    public List<string> HideVoices { get; private set; } = new List<string>();

    public bool SaveCopyOfOriginalFile { get; set; } = true;
}
