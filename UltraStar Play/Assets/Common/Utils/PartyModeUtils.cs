using System.Collections.Generic;
using System.Linq;

public static class PartyModeUtils
{
    public static bool IsFinalRound(PartyModeSceneData partyModeSceneData)
    {
        if (partyModeSceneData == null)
        {
            return false;
        }
        return partyModeSceneData.currentRoundIndex >= partyModeSceneData.PartyModeSettings.RoundCount - 1
               || IsFinalRoundOfKnockOutTournament(partyModeSceneData);
    }

    private static bool IsFinalRoundOfKnockOutTournament(PartyModeSceneData partyModeSceneData)
    {
        if (!partyModeSceneData.PartyModeSettings.TeamSettings.IsKnockOutTournament)
        {
            return false;
        }

        if (partyModeSceneData.PartyModeSettings.TeamSettings.IsFreeForAll)
        {
            List<PlayerProfile> allFreeForAllPlayerProfiles = GetAllPlayerProfiles(partyModeSceneData.PartyModeSettings);
            List<PartyModeTeamSettings> allTeamSettingsOfFreeForAllPlayers = allFreeForAllPlayerProfiles
                .Select(playerProfile => GetTeam(partyModeSceneData, playerProfile))
                .ToList();
            int remainingFreeForAllTeams = allTeamSettingsOfFreeForAllPlayers
                .Select(team => !IsKnockedOut(partyModeSceneData, team))
                .Count();
            return remainingFreeForAllTeams <= 2;
        }
        else
        {
            int remainingTeams = partyModeSceneData.PartyModeSettings.TeamSettings.Teams
                .Select(team => !IsKnockedOut(partyModeSceneData, team))
                .Count();
            return remainingTeams <= 2;
        }
    }

    public static bool IsKnockedOut(PartyModeSceneData partyModeSceneData, PartyModeTeamSettings team)
    {
        if (partyModeSceneData.teamToIsKnockedOutMap.TryGetValue(team, out bool isKnockedOut))
        {
            return isKnockedOut;
        }

        return false;
    }

    public static PartyModeTeamSettings GetTeam(PartyModeSceneData partyModeSceneData, PlayerProfile playerProfile)
    {
        if (partyModeSceneData == null)
        {
            return null;
        }

        if (partyModeSceneData.PartyModeSettings.TeamSettings.IsFreeForAll)
        {
            return GetTeamForPlayerInFreeForAll(partyModeSceneData, playerProfile);
        }

        // Return of this player
        return partyModeSceneData.PartyModeSettings.TeamSettings.Teams.FirstOrDefault(team
            => team.playerProfiles.Contains(playerProfile) || team.guestPlayerProfiles.Contains(playerProfile));
    }

    private static PartyModeTeamSettings GetTeamForPlayerInFreeForAll(PartyModeSceneData partyModeSceneData, PlayerProfile playerProfile)
    {
        if (!partyModeSceneData.freeForAllPlayerToTeam.TryGetValue(playerProfile, out PartyModeTeamSettings team))
        {
            team = new();
            // Team has same name as player
            team.name = playerProfile.Name;
            team.playerProfiles.Add(playerProfile);
            partyModeSceneData.freeForAllPlayerToTeam[playerProfile] = team;
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
        return partyModeSettings.TeamSettings.Teams
            .SelectMany(team => GetAllPlayerProfiles(team))
            .Distinct()
            .ToList();
    }

    public static List<PartyModeTeamSettings> GetLeadingTeams(PartyModeSceneData partyModeSceneData, List<PartyModeTeamSettings> teams)
    {
        if (teams.IsNullOrEmpty())
        {
            return new();
        }

        // Return teams with highest score
        int highestTeamScore = teams.Select(team => GetTeamScore(partyModeSceneData, team)).Max();
        return teams
            .Where(team => GetTeamScore(partyModeSceneData, team) == highestTeamScore)
            .ToList();
    }

    public static int GetTeamScore(PartyModeSceneData partyModeSceneData, PartyModeTeamSettings teamSettings)
    {
        if (teamSettings == null
            || !partyModeSceneData.teamToScoreMap.TryGetValue(teamSettings, out int score))
        {
            return 0;
        }

        return score;
    }

    public static List<PartyModeTeamSettings> GetAllTeams(PartyModeSceneData partyModeSceneData)
    {
        if (partyModeSceneData.PartyModeSettings.TeamSettings.IsFreeForAll)
        {
            List<PlayerProfile> playerProfiles = GetAllPlayerProfiles(partyModeSceneData.PartyModeSettings);
            return playerProfiles
                .Select(playerProfile => GetTeamForPlayerInFreeForAll(partyModeSceneData, playerProfile))
                .ToList();
        }
        else
        {
            return partyModeSceneData.PartyModeSettings.TeamSettings.Teams.ToList();
        }
    }
}
