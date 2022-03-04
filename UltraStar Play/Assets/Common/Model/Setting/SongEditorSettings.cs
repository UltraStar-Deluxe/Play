using System;
using System.Collections.Generic;

[Serializable]
public class SongEditorSettings
{
    public float MusicVolume { get; set; } = 1;
    public float MusicPlaybackSpeed { get; set; } = 1;

    // Recording in SongEditorScene
    public ESongEditorRecordingSource RecordingSource { get; set; }
    public MicProfile MicProfile { get; set; }
    public int MicOctaveOffset { get; set; }
    public int MicDelayInMillis { get; set; } = 450;
    public int MidiNoteForButtonRecording { get; set; } = MidiUtils.MidiNoteConcertPitch;
    public string ButtonDisplayNameForButtonRecording { get; set; } = "N";

    public bool AdjustFollowingNotes { get; set; }

    // Velocity should be between 0 and 127
    public int MidiVelocity { get; set; } = 100;
    // Gain is similar to volume and should be between 0 and 1 to make it more silent and above 1 to make it louder.
    public float MidiGain { get; set; } = 1;
    public bool MidiSoundPlayAlongEnabled { get; set; } = true;
    public int MidiPlaybackOffsetInMillis { get; set; }
    public string LastMidiFilePath { get; set; } = "";

    // Option to show / hide voices.
    // Contains the names of the voices that should be hidden.
    public List<string> HideVoices { get; private set; } = new List<string>();

    public bool SaveCopyOfOriginalFile { get; set; }

    public bool ShowLyricsArea { get; set; } = true;
    public bool ShowVideoArea { get; set; } = true;
    public bool ShowStatusBar { get; set; } = true;
    public bool ShowVirtualPianoArea { get; set; } = true;
    public bool SmallLeftSideBar { get; set; }
    public bool ShowControlHints { get; set; } = true;

    public int GridSizeInDevicePixels { get; set; } = 1;
    public int SentenceLineSizeInDevicePixels { get; set; } = 2;
}
