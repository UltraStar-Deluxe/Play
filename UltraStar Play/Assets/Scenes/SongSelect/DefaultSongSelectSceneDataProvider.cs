using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class DefaultSongSelectSceneDataProvider : MonoBehaviour, IDefaultSceneDataProvider
{
    public bool partyMode;
    public bool isFreeForAll;
    public bool isKnockOutTournament;
    public PartyModeSongSelectionSettings songSelectionSettings;
    public List<string> modifierIds;

    public SceneData GetDefaultSceneData()
    {
        SongMetaManager.Instance.WaitUntilSongScanFinished();

        SongSelectSceneData songSelectSceneData = new();
        if (partyMode)
        {
            songSelectSceneData.partyModeSceneData = CreatePartyModeSceneData();
        }
        return songSelectSceneData;
    }

    private PartyModeSceneData CreatePartyModeSceneData()
    {
        PartyModeSceneData partyModeSceneData = new();
        partyModeSceneData.PartyModeSettings = CreatePartyModeSettings();
        return partyModeSceneData;
    }

    private PartyModeSettings CreatePartyModeSettings()
    {
        Settings settings = SettingsManager.Instance.Settings;
        NonPersistentSettings nonPersistentSettings = SettingsManager.Instance.NonPersistentSettings;
        PartyModeSettings partyModeSettings = new();

        void FillTeams()
        {
            partyModeSettings.TeamSettings.IsFreeForAll = isFreeForAll;
            partyModeSettings.TeamSettings.IsKnockOutTournament = isKnockOutTournament;
            partyModeSettings.TeamSettings.Teams = new();

            // Add first team with normal player profiles
            PartyModeTeamSettings firstTeam = new();
            firstTeam.name = "Team 01";
            firstTeam.playerProfiles = settings.PlayerProfiles.ToList();
            partyModeSettings.TeamSettings.Teams.Add(firstTeam);

            // Add second team with guest player profile
            PlayerProfile guestPlayerProfile = settings.PartyModeSettings.GuestPlayerProfiles.FirstOrDefault();
            if (guestPlayerProfile != null)
            {
                PartyModeTeamSettings secondTeam = new();
                secondTeam.name = "Team 02";
                secondTeam.guestPlayerProfiles = new List<PlayerProfile> { guestPlayerProfile };
                partyModeSettings.TeamSettings.Teams.Add(secondTeam);
            }
        }

        void FillSongSelection()
        {
            partyModeSettings.SongSelectionSettings = songSelectionSettings;
        }

        void FillRounds()
        {
            GameRoundSettings roundSettings = new();
            roundSettings.modifiers = GameRoundModifierRegistry.GetAllById(modifierIds);
            nonPersistentSettings.GameRoundSettings = roundSettings;

            partyModeSettings.RoundCount = 2;
        }

        FillTeams();
        FillSongSelection();
        FillRounds();
        return partyModeSettings;
    }
}
