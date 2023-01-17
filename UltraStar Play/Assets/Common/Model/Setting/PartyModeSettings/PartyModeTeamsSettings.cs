using System;
using System.Collections.Generic;

[Serializable]
public class PartyModeTeamsSettings
{
    public List<PartyModeTeamSettings> teams = new();
    public bool isFreeForAll;
    public bool isKnockOutTournament;
}
