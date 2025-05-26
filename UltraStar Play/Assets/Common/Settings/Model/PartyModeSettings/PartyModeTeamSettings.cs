using System;
using System.Collections.Generic;

[Serializable]
public class PartyModeTeamSettings
{
    public string name = "";
    public List<PlayerProfile> playerProfiles = new();
    public List<PlayerProfile> guestPlayerProfiles = new();
}
