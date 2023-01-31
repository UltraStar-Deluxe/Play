using System;
using System.Collections.Generic;

[Serializable]
public class PartyModeSettings
{
    public int currentRoundIndex;
    public PartyModeTeamsSettings teamSettings = new();
    public PartyModeSongSelectionSettings songSelectionSettings = new();
    public PartyModeRoundsSettings roundsSettings = new();

    public List<PlayerProfile> guestPlayerProfiles = new();
    public List<PartyModeRoundSettingsPreset> roundSettingsPresets = new();

    public GameRoundSettings CurrentRoundSettings => roundsSettings.gameRoundSettings[currentRoundIndex];

    public Dictionary<PartyModeTeamSettings, int> teamToScoreMap = new();
}
