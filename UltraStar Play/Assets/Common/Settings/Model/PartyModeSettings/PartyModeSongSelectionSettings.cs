using System;

[Serializable]
public class PartyModeSongSelectionSettings
{
    public EPartyModeSongSelectionMode SongSelectionMode { get; set; } = EPartyModeSongSelectionMode.Manual;
    public string SongPoolPlaylistName { get; set; } = "";
    public int JokerCount { get; set; } = 5;
}
