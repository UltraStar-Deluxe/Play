using System.Collections.Generic;

public class MidiFileSongMeta : UltraStarSongMeta
{
    public MidiFileSongMeta()
    {
        DoLoadVoices = DefaultDoLoadVoices;
    }

    public MidiFileSongMeta(SongMeta other)
        : base(other)
    {
        DoLoadVoices = DefaultDoLoadVoices;
    }

    public MidiFileSongMeta(
        string artist,
        string title,
        float txtFileBpm,
        string audioFile,
        Dictionary<EVoiceId, string> voiceIdToDisplayName)
            : base(artist, title, txtFileBpm, audioFile, voiceIdToDisplayName)
    {
        DoLoadVoices = DefaultDoLoadVoices;
    }

    private void DefaultDoLoadVoices()
    {
        MidiToSongMetaUtils.FillSongMetaWithMidiLyricsAndNotes(this);
    }
}
