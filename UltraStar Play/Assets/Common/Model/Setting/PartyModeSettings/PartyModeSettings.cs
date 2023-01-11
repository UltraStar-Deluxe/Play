public class PartyModeSettings
{
    public PartyModeTeamsSettings TeamSettings { get; set; } = new();
    public PartyModeSongSelectionSettings SongSelectionSettings { get; set; } = new();
    public PartyModeRoundsSettings RoundsSettings { get; set; } = new();
}
