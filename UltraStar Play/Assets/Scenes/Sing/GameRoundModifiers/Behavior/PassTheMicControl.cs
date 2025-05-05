using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PassTheMicControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private SingSceneControl singSceneControl;

    [Inject]
    private Settings settings;

    [Inject(UxmlName = R.UxmlNames.passTheMicProgressBar)]
    private ProgressBar passTheMicProgressBar;

    private float passTheMicTimeInSeconds;

    private readonly Dictionary<PartyModeTeamSettings, PlayerProfile> teamToCurrentPlayerProfile = new();
    private readonly Dictionary<PartyModeTeamSettings, PlayerProfile> teamToNextPlayerProfile = new();
    private readonly Dictionary<PartyModeTeamSettings, List<PlayerProfile>> teamToUsedPlayerProfiles = new();

    private bool isInjectionFinished;

    public void OnInjectionFinished()
    {
        isInjectionFinished = true;
        passTheMicProgressBar.SetVisibleByDisplay(singSceneControl.IsPassTheMic);
        if (!singSceneControl.HasPartyModeSceneData
            || !singSceneControl.IsPassTheMic)
        {
            return;
        }

        singSceneControl.PlayerControls.ForEach(playerControl =>
        {
            PartyModeTeamSettings team = PartyModeUtils.GetTeam(singSceneControl.PartyModeSceneData, playerControl.PlayerProfile);
            if (team != null)
            {
                teamToCurrentPlayerProfile[team] = playerControl.PlayerProfile;
            }
        });

        // Choose next players
        ChooseInitialNextPlayers();
    }

    public void Update()
    {
        if (!isInjectionFinished
            || !singSceneControl.IsPassTheMic)
        {
            return;
        }

        passTheMicTimeInSeconds += Time.deltaTime;
        if (passTheMicTimeInSeconds >= settings.PassTheMicTimeInSeconds)
        {
            passTheMicTimeInSeconds -= settings.PassTheMicTimeInSeconds;
            singSceneControl.PartyModeSettings.TeamSettings.Teams.ForEach(team => PassTheMicToNextPlayerInTeam(team));
        }

        UpdateProgressBar();
    }

    private void UpdateProgressBar()
    {
        passTheMicProgressBar.value = 100 * passTheMicTimeInSeconds / settings.PassTheMicTimeInSeconds;
    }

    private void PassTheMicToNextPlayerInTeam(PartyModeTeamSettings team)
    {
        SetNextPlayerAsCurrentPlayer(team);
        ChooseNextPlayer(team);
    }

    private void ChooseNextPlayer(PartyModeTeamSettings team)
    {
        // Determine used players
        teamToCurrentPlayerProfile.TryGetValue(team, out PlayerProfile currentPlayerProfile);
        if (!teamToUsedPlayerProfiles.TryGetValue(team, out List<PlayerProfile> usedPlayerProfiles))
        {
            usedPlayerProfiles = new();
            teamToUsedPlayerProfiles[team] = usedPlayerProfiles;
        }
        if (currentPlayerProfile != null)
        {
            usedPlayerProfiles.AddIfNotContains(currentPlayerProfile);
        }

        // Select new next player randomly. Players that did not sing yet are preferred.
        List<PlayerProfile> unusedPlayerProfiles = PartyModeUtils.GetAllPlayerProfiles(team)
            .Except(usedPlayerProfiles)
            .ToList();
        if (unusedPlayerProfiles.IsNullOrEmpty())
        {
            // All players of the team did sing. Start new iteration over players of the team.
            unusedPlayerProfiles = PartyModeUtils.GetAllPlayerProfiles(team);
            usedPlayerProfiles.Clear();
        }

        PlayerProfile nextPlayerProfile = RandomUtils.RandomOf(unusedPlayerProfiles);
        teamToNextPlayerProfile[team] = nextPlayerProfile;
        UpdateNextPlayerUi(team);
    }

    private void ChooseInitialNextPlayers()
    {
        singSceneControl.PartyModeSettings.TeamSettings.Teams.ForEach(team => ChooseNextPlayer(team));
    }

    private void SetNextPlayerAsCurrentPlayer(PartyModeTeamSettings team)
    {
        if (!teamToUsedPlayerProfiles.TryGetValue(team, out List<PlayerProfile> usedPlayerProfiles))
        {
            usedPlayerProfiles = new();
            teamToUsedPlayerProfiles[team] = usedPlayerProfiles;
        }

        // Remember that the current player did sing already.
        if (teamToCurrentPlayerProfile.TryGetValue(team, out PlayerProfile currentPlayerProfile))
        {
            usedPlayerProfiles.Add(currentPlayerProfile);
        }

        // Activate next player.
        if (teamToNextPlayerProfile.TryGetValue(team, out PlayerProfile nextPlayerProfile))
        {
            ActivatePlayer(team, nextPlayerProfile);
        }
    }

    private void ActivatePlayer(PartyModeTeamSettings team, PlayerProfile playerProfile)
    {
        if (teamToCurrentPlayerProfile.TryGetValue(team, out PlayerProfile currentPlayerProfile)
            && currentPlayerProfile == playerProfile)
        {
            // Nothing to do.
            return;
        }

        teamToCurrentPlayerProfile[team] = playerProfile;
        UpdateCurrentPlayerUi(team);
    }

    private void UpdateCurrentPlayerUi(PartyModeTeamSettings team)
    {
        if (!teamToCurrentPlayerProfile.TryGetValue(team, out PlayerProfile currentPlayerProfile))
        {
            return;
        }

        PlayerControl playerControlOfTeam = GetPlayerControlOfTeam(team);
        if (playerControlOfTeam == null)
        {
            return;
        }
        playerControlOfTeam.PlayerUiControl.SetPlayerProfile(currentPlayerProfile);
    }

    private void UpdateNextPlayerUi(PartyModeTeamSettings team)
    {
        if (!teamToNextPlayerProfile.TryGetValue(team, out PlayerProfile nextPlayerProfile))
        {
            return;
        }

        PlayerControl playerControlOfTeam = GetPlayerControlOfTeam(team);
        if (playerControlOfTeam == null)
        {
            return;
        }
        playerControlOfTeam.PlayerUiControl.SetNextPlayerProfile(nextPlayerProfile);
    }

    private PlayerControl GetPlayerControlOfTeam(PartyModeTeamSettings team)
    {
        if (!teamToCurrentPlayerProfile.TryGetValue(team, out PlayerProfile currentPlayerProfile))
        {
            return null;
        }
        return singSceneControl.PlayerControls.FirstOrDefault(playerControl
            => PartyModeUtils.GetTeam(singSceneControl.PartyModeSceneData, playerControl.PlayerProfile)
               == PartyModeUtils.GetTeam(singSceneControl.PartyModeSceneData, currentPlayerProfile));
    }
}
