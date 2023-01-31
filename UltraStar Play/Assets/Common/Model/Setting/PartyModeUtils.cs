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
        return partyModeSettings.currentRoundIndex >= partyModeSettings.roundsSettings.gameRoundSettings.Count - 1
               || IsFinalRoundOfKnockOutTournament(partyModeSettings);
    }

    private static bool IsFinalRoundOfKnockOutTournament(PartyModeSettings partyModeSettings)
    {
        if (!partyModeSettings.teamSettings.isKnockOutTournament)
        {
            return false;
        }

        if (partyModeSettings.teamSettings.isFreeForAll)
        {
            List<PlayerProfile> allFreeForAllPlayerProfiles = GetAllPlayerProfiles(partyModeSettings);
            List<PartyModeTeamSettings> allTeamSettingsOfFreeForAllPlayers = allFreeForAllPlayerProfiles
                .Select(playerProfile => GetTeam(partyModeSettings, playerProfile))
                .ToList();
            int remainingFreeForAllTeams = allTeamSettingsOfFreeForAllPlayers.Select(team => !team.isKnockedOut).Count();
            return remainingFreeForAllTeams <= 2;
        }
        else
        {
            int remainingTeams = partyModeSettings.teamSettings.teams.Select(team => !team.isKnockedOut).Count();
            return remainingTeams <= 2;
        }
    }

    public static PartyModeTeamSettings GetTeam(PartyModeSettings partyModeSettings, PlayerProfile playerProfile)
    {
        if (partyModeSettings == null)
        {
            return null;
        }

        if (partyModeSettings.teamSettings.isFreeForAll)
        {
            return GetTeamForPlayerInFreeForAll(partyModeSettings, playerProfile);
        }

        // Return of this player
        return partyModeSettings.teamSettings.teams.FirstOrDefault(team
            => team.playerProfiles.Contains(playerProfile) || team.guestPlayerProfiles.Contains(playerProfile));
    }

    private static PartyModeTeamSettings GetTeamForPlayerInFreeForAll(PartyModeSettings partyModeSettings, PlayerProfile playerProfile)
    {
        if (!partyModeSettings.teamSettings.freeForAllPlayerToTeam.TryGetValue(playerProfile, out PartyModeTeamSettings team))
        {
            team = new();
            // Team has same name as player
            team.name = playerProfile.Name;
            team.playerProfiles.Add(playerProfile);
            partyModeSettings.teamSettings.freeForAllPlayerToTeam[playerProfile] = team;
        }

        return team;
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

    public static List<PlayerProfile> GetAllPlayerProfiles(PartyModeSettings partyModeSettings)
    {
        return partyModeSettings.teamSettings.teams
            .SelectMany(team => GetAllPlayerProfiles(team))
            .Distinct()
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

    public static List<PartyModeTeamSettings> GetAllTeams(PartyModeSettings partyModeSettings)
    {
        if (partyModeSettings.teamSettings.isFreeForAll)
        {
            List<PlayerProfile> playerProfiles = GetAllPlayerProfiles(partyModeSettings);
            return playerProfiles
                .Select(playerProfile => GetTeamForPlayerInFreeForAll(partyModeSettings, playerProfile))
                .ToList();
        }
        else
        {
            return partyModeSettings.teamSettings.teams.ToList();
        }
    }
}
