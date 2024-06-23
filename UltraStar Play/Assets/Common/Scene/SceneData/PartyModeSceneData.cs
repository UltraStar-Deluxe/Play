using System.Collections.Generic;

public class PartyModeSceneData : SceneData
{
    public PartyModeSettings PartyModeSettings { get; set; }

    public int currentRoundIndex;
    public int remainingJokerCount;
    public Dictionary<PlayerProfile, PartyModeTeamSettings> freeForAllPlayerToTeam = new();
    public Dictionary<PartyModeTeamSettings, int> teamToScoreMap = new();
    public Dictionary<PartyModeTeamSettings, bool> teamToIsKnockedOutMap = new();
}
