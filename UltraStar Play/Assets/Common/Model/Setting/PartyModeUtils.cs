using System.Collections.Generic;
using System.Linq;

public static class PartyModeUtils
{
    public static bool IsFinalRound(PartyModeSettings partyModeSettings)
    {
        if (partyModeSettings == null)
        {
            return false;
        }
        return partyModeSettings.currentRoundIndex >= partyModeSettings.roundsSettings.gameRoundSettings.Count - 1;
    }

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
        if (teamSettings == null)
        {
            return new();
        }
        return teamSettings.playerProfiles
            .Union(teamSettings.guestPlayerProfiles)
            .ToList();
    }

    public static List<PartyModeTeamSettings> GetLeadingTeams(PartyModeSettings partyModeSettings, List<PartyModeTeamSettings> teams)
    {
        if (teams.IsNullOrEmpty())
        {
            return new();
        }

        // Return teams with highest score
        int highestTeamScore = teams.Select(team => GetTeamScore(partyModeSettings, team)).Max();
        return teams
            .Where(team => GetTeamScore(partyModeSettings, team) == highestTeamScore)
            .ToList();
    }

    public static int GetTeamScore(PartyModeSettings partyModeSettings, PartyModeTeamSettings teamSettings)
    {
        if (partyModeSettings.teamToScoreMap.TryGetValue(teamSettings, out int score))
        {
            return score;
        }

        return 0;
    }
}
