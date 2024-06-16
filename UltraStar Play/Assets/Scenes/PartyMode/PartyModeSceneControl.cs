using System;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PartyModeSceneControl : MonoBehaviour, INeedInjection, IBinder, IInjectionFinishedListener
{
    [InjectedInInspector]
    public VisualTreeAsset valueInputDialogUi;

    [InjectedInInspector]
    public VisualTreeAsset teamColumnUi;

    [InjectedInInspector]
    public VisualTreeAsset teamColumnPlayerUi;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private Injector injector;

    [Inject]
    private Settings settings;

    [Inject]
    private PartyModeSceneData sceneData;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private PlaylistManager playlistManager;

    [Inject(UxmlName = R.UxmlNames.partyModeTeamsConfigUi)]
    private VisualElement partyModeTeamsConfigUi;

    [Inject(UxmlName = R.UxmlNames.partyModeSongSelectionConfigUi)]
    private VisualElement partyModeSongSelectionConfigUi;

    [Inject(UxmlName = R.UxmlNames.backButton)]
    private Button backButton;

    [Inject(UxmlName = R.UxmlNames.continueButton)]
    private Button continueButton;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    private PartyModeSettings PartyModeSettings => sceneData.PartyModeSettings;

    private readonly PartyModeTeamConfigControl teamConfigControl = new();
    private readonly PartyModeSongSelectionConfigControl songSelectionConfigControl = new();

    public void OnInjectionFinished()
    {
        songMetaManager.ScanSongsIfNotDoneYet();

        InitPartyModeSettings();

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable()
            .Subscribe(_ => OnBack());

        backButton.RegisterCallbackButtonTriggered(_ => OnBack());
        continueButton.RegisterCallbackButtonTriggered(_ => OnContinue());
        continueButton.Focus();

        // Inject child controls
        injector.Inject(teamConfigControl);
        injector.Inject(songSelectionConfigControl);
    }

    private void InitPartyModeSettings()
    {
        sceneData.PartyModeSettings = settings.PartyModeSettings;

        // Add at least two teams
        for (int i = PartyModeSettings.TeamSettings.Teams.Count; i < 2; i++)
        {
            PartyModeTeamSettings newTeam = new();
            newTeam.name = PartyModeTeamConfigControl.GetDefaultTeamName(newTeam, PartyModeSettings);
            PartyModeSettings.TeamSettings.Teams.Add(newTeam);
        }

        // Add all players that are selected for singing to the teams.
        // First, add the regular player profiles. Afterwards, add the guest profiles.
        AddPlayerProfilesToTeams(false);
        AddPlayerProfilesToTeams(true);

        // Add at least one round
        if (PartyModeSettings.RoundCount <= 0)
        {
            PartyModeSettings.RoundCount = 1;
        }

        // Select the "all songs" playlist
        PartyModeSettings.SongSelectionSettings.SongPoolPlaylist = UltraStarAllSongsPlaylist.Instance;
    }

    private void OnBack()
    {
        sceneNavigator.LoadScene(EScene.MainScene);
	}

    private void OnContinue()
    {
        Translation errorMessage;

        errorMessage = GetSongSelectionConfigErrorMessage();
        if (!errorMessage.Value.IsNullOrEmpty())
        {
            NotificationManager.CreateNotification(errorMessage);
            return;
        }

        errorMessage = GetTeamsConfigErrorMessage();
        if (!errorMessage.Value.IsNullOrEmpty())
        {
            NotificationManager.CreateNotification(errorMessage);
            return;
        }

        errorMessage = GetRoundsConfigErrorMessage();
        if (!errorMessage.Value.IsNullOrEmpty())
        {
            NotificationManager.CreateNotification(errorMessage);
            return;
        }

        FinishScene();
    }

    private void FinishScene()
    {
        // Select all enabled player profiles for singing
        settings.PlayerProfiles.ForEach(playerProfile => playerProfile.IsSelected = playerProfile.IsEnabled);

        // Reset scene data
        sceneData.teamToIsKnockedOutMap.Clear();
        sceneData.freeForAllPlayerToTeam.Clear();
        sceneData.teamToScoreMap.Clear();
        sceneData.currentRoundIndex = 0;
        sceneData.remainingJokerCount = PartyModeSettings.SongSelectionSettings.JokerCount;

        // Start next scene
        SongSelectSceneData songSelectSceneData = new();
        songSelectSceneData.partyModeSceneData = sceneData;
        sceneNavigator.LoadScene(EScene.SongSelectScene, songSelectSceneData);
    }

    private Translation GetSongSelectionConfigErrorMessage()
    {
        if (PartyModeSettings.SongSelectionSettings.SongPoolPlaylist == null
            || PartyModeSettings.SongSelectionSettings.SongPoolPlaylist.IsEmpty)
        {
            return Translation.Get(R.Messages.partyMode_error_emptyPlaylist);
        }

        return Translation.Empty;
    }

    private Translation GetTeamsConfigErrorMessage()
    {
        if (PartyModeSettings.TeamSettings.Teams.Count < 1)
        {
            return Translation.Get(R.Messages.partyMode_error_tooFewTeams);
        }

        if (PartyModeSettings.TeamSettings.Teams
            .AnyMatch(team => team.playerProfiles.IsNullOrEmpty() && team.guestPlayerProfiles.IsNullOrEmpty()))
        {
            return Translation.Get(R.Messages.partyMode_error_tooFewPlayers);
        }

        return Translation.Empty;
    }

    private Translation GetRoundsConfigErrorMessage()
    {
        if (PartyModeSettings.RoundCount <= 0)
        {
            return Translation.Get(R.Messages.partyMode_error_tooFewRounds);
        }

        if (PartyModeSettings.TeamSettings.IsKnockOutTournament)
        {
            if (PartyModeSettings.TeamSettings.IsFreeForAll)
            {
                // N players => at most (N - 1) rounds to play, until there is a single winning player.
                int playerCount = PartyModeSettings.TeamSettings.Teams
                    .Select(team => team.playerProfiles.Count + team.guestPlayerProfiles.Count)
                    .Sum();
                if (PartyModeSettings.RoundCount >= playerCount)
                {
                    return Translation.Get(R.Messages.partyMode_error_tooManyRoundsForKo);
                }
            }
            else
            {
                // N teams => at most (N - 1) rounds to play, until there is a single winning team.
                int teamCount = PartyModeSettings.TeamSettings.Teams.Count;
                if (PartyModeSettings.RoundCount >= teamCount)
                {
                    return Translation.Get(R.Messages.partyMode_error_tooManyRoundsForKo);
                }
            }
        }

        return Translation.Empty;
    }

    private void AddPlayerProfilesToTeams(bool guests)
    {
        PartyModeSettings.TeamSettings.Teams.ForEach(teamList =>
        {
            if (guests)
            {
                teamList.guestPlayerProfiles.Clear();
            }
            else
            {
                teamList.playerProfiles.Clear();
            }
        });

        List<PlayerProfile> allRelevantPlayerProfiles = settings.PlayerProfiles
            .Where(playerProfile => playerProfile.IsEnabled)
            .Union(PartyModeSettings.GuestPlayerProfiles)
            .ToList();
        if (allRelevantPlayerProfiles.IsNullOrEmpty())
        {
            PartyModeSettings.TeamSettings.Teams.ForEach(team =>
            {
                // No players to assign
                team.playerProfiles.Clear();
                team.guestPlayerProfiles.Clear();
            });
            return;
        }

        List<PlayerProfile> playerProfiles = guests
            ? PartyModeSettings.GuestPlayerProfiles
            : settings.PlayerProfiles;
        List<PlayerProfile> relevantPlayerProfiles = playerProfiles
            .Where(playerProfile => playerProfile.IsEnabled)
            .ToList();
        List<PlayerProfile> assignedPlayerProfiles = PartyModeSettings.TeamSettings.Teams
            .SelectMany(team => team.playerProfiles.Union(team.guestPlayerProfiles))
            .ToList();
        List<PlayerProfile> relevantUnassignedPlayerProfiles = relevantPlayerProfiles
            .Where(playerProfile => !assignedPlayerProfiles.Contains(playerProfile))
            .ToList();
        foreach (PlayerProfile playerProfile in relevantUnassignedPlayerProfiles)
        {
            int playerProfileIndex = allRelevantPlayerProfiles.IndexOf(playerProfile);
            double playerProfilePercent = playerProfileIndex / (double)(allRelevantPlayerProfiles.Count - 1);
            int teamIndex = (int)Math.Round(playerProfilePercent * (PartyModeSettings.TeamSettings.Teams.Count - 1));
            teamIndex = NumberUtils.Limit(teamIndex, 0, PartyModeSettings.TeamSettings.Teams.Count - 1);
            PartyModeTeamSettings team = PartyModeSettings.TeamSettings.Teams[teamIndex];
            List<PlayerProfile> targetList = guests ? team.guestPlayerProfiles : team.playerProfiles;
            targetList.Add(playerProfile);
        }
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new();
        bb.BindExistingInstance(this);
        bb.BindExistingInstance(gameObject);
        bb.BindExistingInstance(SceneNavigator.GetSceneData(CreateDefaultPartyModeSceneData()));
        bb.BindExistingInstance(teamConfigControl);
        bb.BindExistingInstance(songSelectionConfigControl);
        bb.Bind(nameof(valueInputDialogUi)).ToExistingInstance(valueInputDialogUi);
        bb.Bind(nameof(teamColumnUi)).ToExistingInstance(teamColumnUi);
        bb.Bind(nameof(teamColumnPlayerUi)).ToExistingInstance(teamColumnPlayerUi);
        return bb.GetBindings();
    }

    private PartyModeSceneData CreateDefaultPartyModeSceneData()
    {
        PartyModeSceneData newPartyModeSceneData = new();
        newPartyModeSceneData.PartyModeSettings = SettingsManager.Instance.Settings.PartyModeSettings;
        return newPartyModeSceneData;
    }
}
