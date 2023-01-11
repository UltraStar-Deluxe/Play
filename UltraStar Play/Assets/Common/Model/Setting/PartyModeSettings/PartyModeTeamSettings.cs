using System.Collections.Generic;

public class PartyModeTeamSettings
{
    public string Name { get; set; } = "";
    public List<PlayerProfile> PlayerProfiles { get; set; } = new();
    public List<PlayerProfile> GuestPlayerProfiles { get; set; } = new();
}
