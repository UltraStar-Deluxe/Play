using System.Collections.Generic;
using System.Linq;

public static class PartyModeUtils
{
    public static PartyModeTeamSettings GetTeam(PartyModeSettings partyModeSettings, PlayerProfile playerProfile)
    {
        if (partyModeSettings == null)
        {
            return null;
        }

        return partyModeSettings.teamSettings.teams.FirstOrDefault(team
            => team.playerProfiles.Contains(playerProfile) || team.guestPlayerProfiles.Contains(playerProfile));
    }

    public static List<PlayerProfile> GetAllPlayerProfiles(PartyModeTeamSettings teamSettings)
    {
        return teamSettings.playerProfiles
            .Union(teamSettings.guestPlayerProfiles)
            .ToList();
    }
}
