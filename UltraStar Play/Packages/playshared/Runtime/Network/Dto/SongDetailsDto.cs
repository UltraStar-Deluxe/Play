using System.Collections.Generic;

public class SongDetailsDto : JsonSerializable
{
    public string SongId { get; set; }
    public bool IsFavorite { get; set; }
    public Dictionary<string, string> VoiceDisplayNameToLyricsMap { get; set; }
}
