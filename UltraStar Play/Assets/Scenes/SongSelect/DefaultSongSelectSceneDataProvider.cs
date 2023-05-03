using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class DefaultSongSelectSceneDataProvider : MonoBehaviour, IDefaultSceneDataProvider
{
    public bool partyMode;
    public bool isFreeForAll;
    public bool isKnockOutTournament;
    public PartyModeSongSelectionSettings songSelectionSettings;
    public GameRoundFinishConditionSettings finishConditionSettings;
    public List<EGameRoundModifier> modifiers;
    public GameRoundModifierConditionSettings modifierConditionSettings;

    public SceneData GetDefaultSceneData()
    {
        SongMetaManager.Instance.ScanFilesIfNotDoneYet();
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
            partyModeSettings.teamSettings.isFreeForAll = isFreeForAll;
            partyModeSettings.teamSettings.isKnockOutTournament = isKnockOutTournament;
            partyModeSettings.teamSettings.teams = new();

            // Add first team with normal player profiles
            PartyModeTeamSettings firstTeam = new();
            firstTeam.name = "Team 01";
            firstTeam.playerProfiles = settings.PlayerProfiles.ToList();
            partyModeSettings.teamSettings.teams.Add(firstTeam);

            // Add second team with guest player profile
            PlayerProfile guestPlayerProfile = settings.PartyModeSettings.guestPlayerProfiles.FirstOrDefault();
            if (guestPlayerProfile != null)
            {
                PartyModeTeamSettings secondTeam = new();
                secondTeam.name = "Team 02";
                secondTeam.guestPlayerProfiles = new List<PlayerProfile> { guestPlayerProfile };
                partyModeSettings.teamSettings.teams.Add(secondTeam);
            }
        }

        void FillSongSelection()
        {
            partyModeSettings.songSelectionSettings = songSelectionSettings;
        }

        void FillRounds()
        {
            GameRoundSettings roundSettings = new();
            roundSettings.modifiers = modifiers.ToHashSet();
            roundSettings.finishConditionSettings = finishConditionSettings;
            roundSettings.modifierConditionSettings = modifierConditionSettings;
            nonPersistentSettings.GameRoundSettings = roundSettings;

            partyModeSettings.roundCount = 2;
        }

        FillTeams();
        FillSongSelection();
        FillRounds();
        return partyModeSettings;
    }
}
