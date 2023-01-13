using System.Collections.Generic;

public class PartyModeSettings
{
    public PartyModeTeamsSettings TeamSettings { get; set; } = new();
    public PartyModeSongSelectionSettings SongSelectionSettings { get; set; } = new();
    public PartyModeRoundsSettings RoundsSettings { get; set; } = new();

    public List<PlayerProfile> GuestPlayerProfiles { get; set; } = new();
    public List<PartyModeRoundSettingsPreset> RoundSettingsPresets { get; set; } = new();
}
