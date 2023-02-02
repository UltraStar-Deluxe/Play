using System;
using System.Collections.Generic;

[Serializable]
public class PartyModeSettings
{
    public PartyModeTeamsSettings teamSettings = new();
    public PartyModeSongSelectionSettings songSelectionSettings = new();
    public PartyModeRoundsSettings roundsSettings = new();

    public List<PlayerProfile> guestPlayerProfiles = new();
    public List<PartyModeRoundSettingsPreset> roundSettingsPresets = new();
}
