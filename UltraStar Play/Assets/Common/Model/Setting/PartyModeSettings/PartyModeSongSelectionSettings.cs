public class PartyModeSongSelectionSettings
{
    public EPartyModeSongSelectionMode SongSelectionModeMode { get; set; } = EPartyModeSongSelectionMode.Manual;
    public UltraStarPlaylist SongPoolPlaylist { get; set; } = new UltraStarAllSongsPlaylist();
    public int JokerCount { get; set; } = 5;
}
