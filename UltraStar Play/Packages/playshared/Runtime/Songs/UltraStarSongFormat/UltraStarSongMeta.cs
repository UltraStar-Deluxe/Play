using System;
using System.Collections.Generic;

public class UltraStarSongMeta : SongMeta
{
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

        this.voiceIdToDisplayName = voiceIdToDisplayName ?? throw new ArgumentNullException(nameof(voiceIdToDisplayName));
    }

    protected override List<Voice> DoLoadVoices()
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
