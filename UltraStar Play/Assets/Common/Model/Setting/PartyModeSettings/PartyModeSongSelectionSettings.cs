public class PartyModeSongSelectionSettings
{
    public EPartyModeSongSelectionMode SongSelectionMode { get; set; } = EPartyModeSongSelectionMode.Manual;
    public UltraStarPlaylist SongPoolPlaylist { get; set; } = UltraStarAllSongsPlaylist.Instance;
    public int JokerCount { get; set; } = 5;
}
