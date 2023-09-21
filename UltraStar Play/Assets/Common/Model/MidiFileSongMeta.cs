using System;
using System.Collections.Generic;

public class MidiFileSongMeta : UltraStarSongMeta
{
    public MidiFileSongMeta(SongMeta other) : base(other)
    {
    }

    public MidiFileSongMeta(
        string artist,
        string title,
        float txtFileBpm,
        string audioFile,
        Dictionary<EVoiceId, string> voiceIdToDisplayName)
            : base(artist, title, txtFileBpm, audioFile, voiceIdToDisplayName)
    {
    }

    protected override void LoadVoicesFromFile()
    {
        if (HasFailedToLoadVoices)
        {
            return;
        }

        // Do not attempt to load voices again.
        hasLoadedVoices = true;

        // This field is not reset if any errors occurred.
        HasFailedToLoadVoices = true;

        MidiToSongMetaUtils.FillSongMetaWithMidiLyricsAndNotes(this);

        // All done without errors.
        HasFailedToLoadVoices = false;
    }
}
