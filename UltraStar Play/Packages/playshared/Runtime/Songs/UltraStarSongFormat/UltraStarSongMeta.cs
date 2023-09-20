using System;
using System.Collections.Generic;

public class UltraStarSongMeta : SongMeta
{
    public override int VoiceCount => !voiceIdToDisplayName.IsNullOrEmpty()
        ? voiceIdToDisplayName.Count
        : Voices.Count;

    public UltraStarSongMeta(
        string artist,
        string title,
        float bpm,
        string audioFile,
        Dictionary<string, string> voiceIdToDisplayName)
    {
        Artist = artist ?? throw new ArgumentNullException(nameof(artist));
        Bpm = bpm;
        Mp3 = audioFile ?? throw new ArgumentNullException(nameof(audioFile));
        Title = title ?? throw new ArgumentNullException(nameof(title));

        if (voiceIdToDisplayName == null)
        {
            throw new ArgumentNullException(nameof(voiceIdToDisplayName));
        }
        this.voiceIdToDisplayName.AddRange(voiceIdToDisplayName);
    }

    protected override List<Voice> LoadVoices()
    {
        if (FileInfo == null
            || !FileInfo.Exists)
        {
            return new List<Voice>();
        }

        bool.TryParse(GetUnknownHeaderEntry("relative"), out bool isRelativeSongFormat);

        return UltraStarSongVoicesParser.ParseSongFile(
            FileInfo.FullName,
            FileEncoding,
            isRelativeSongFormat,
            false);
    }
}
