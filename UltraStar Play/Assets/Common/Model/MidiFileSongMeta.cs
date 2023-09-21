using System.Collections.Generic;

public class MidiFileSongMeta : UltraStarSongMeta
{
    public MidiFileSongMeta(SongMeta other)
        : base(other)
    {
        OnLoadVoices = DoLoadVoices;
    }

    public MidiFileSongMeta(
        string artist,
        string title,
        float txtFileBpm,
        string audioFile,
        Dictionary<EVoiceId, string> voiceIdToDisplayName)
            : base(artist, title, txtFileBpm, audioFile, voiceIdToDisplayName)
    {
        OnLoadVoices = DoLoadVoices;
    }

    private void DoLoadVoices()
    {
        MidiToSongMetaUtils.FillSongMetaWithMidiLyricsAndNotes(this);
    }
}
