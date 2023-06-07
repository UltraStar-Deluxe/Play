using System;
using System.Collections.Generic;

[Serializable]
public class PartyModeTeamsSettings
{
    public List<PartyModeTeamSettings> Teams { get; set; } = new();
    public bool IsFreeForAll { get; set; }
    public bool IsKnockOutTournament { get; set; }
}
