using System;
using System.Collections.Generic;

[Serializable]
public class PartyModeSettings
{
    public PartyModeTeamsSettings TeamSettings { get; set; } = new();
    public PartyModeSongSelectionSettings SongSelectionSettings { get; set; } = new();

    public List<PlayerProfile> GuestPlayerProfiles { get; set; } = new();
    public List<PartyModeRoundSettingsPreset> RoundSettingsPresets { get; set; } = new();
    
    public int RoundCount { get; set; } = 5;
}
