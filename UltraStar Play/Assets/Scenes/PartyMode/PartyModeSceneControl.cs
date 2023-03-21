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
        songMetaManager.ScanFilesIfNotDoneYet();

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
        for (int i = PartyModeSettings.teamSettings.teams.Count; i < 2; i++)
        {
            PartyModeTeamSettings newTeam = new();
            newTeam.name = PartyModeTeamConfigControl.GetDefaultTeamName(newTeam, PartyModeSettings);
            PartyModeSettings.teamSettings.teams.Add(newTeam);
        }

        // Add all players that are selected for singing to the teams.
        // First, add the regular player profiles. Afterwards, add the guest profiles.
        AddPlayerProfilesToTeams(false);
        AddPlayerProfilesToTeams(true);

        // Add at least one round
        if (PartyModeSettings.roundCount <= 0)
        {
            PartyModeSettings.roundCount = 1;
        }

        // Select the "all songs" playlist
        PartyModeSettings.songSelectionSettings.songPoolPlaylist = UltraStarAllSongsPlaylist.Instance;
    }

    private void OnBack()
    {
        sceneNavigator.LoadScene(EScene.MainScene);
	}

    private void OnContinue()
    {
        string errorMessage;
        
        errorMessage = GetSongSelectionConfigErrorMessage();
        if (!errorMessage.IsNullOrEmpty())
        {
            UiManager.CreateNotification(errorMessage);
            return;
        }
        
        errorMessage = GetTeamsConfigErrorMessage();
        if (!errorMessage.IsNullOrEmpty())
        {
            UiManager.CreateNotification(errorMessage);
            return;
        }

        errorMessage = GetRoundsConfigErrorMessage();
        if (!errorMessage.IsNullOrEmpty())
        {
            UiManager.CreateNotification(errorMessage);
            return;
        }

        FinishScene();
    }

    private void FinishScene()
    {
        // Reset scene data
        sceneData.teamToIsKnockedOutMap.Clear();
        sceneData.freeForAllPlayerToTeam.Clear();
        sceneData.teamToScoreMap.Clear();
        sceneData.currentRoundIndex = 0;
        sceneData.remainingJokerCount = PartyModeSettings.songSelectionSettings.jokerCount;

        // Start next scene
        SongSelectSceneData songSelectSceneData = new();
        songSelectSceneData.partyModeSceneData = sceneData;
        sceneNavigator.LoadScene(EScene.SongSelectScene, songSelectSceneData);
    }

    private string GetSongSelectionConfigErrorMessage()
    {
        if (PartyModeSettings.songSelectionSettings.songPoolPlaylist == null
            || PartyModeSettings.songSelectionSettings.songPoolPlaylist.IsEmpty)
        {
            return "Select a playlist that is not empty";
        }

        return "";
    }

    private string GetTeamsConfigErrorMessage()
    {
        if (PartyModeSettings.teamSettings.teams.Count < 1)
        {
            return "Must use at least one teams";
        }

        if (PartyModeSettings.teamSettings.teams
            .AnyMatch(team => team.playerProfiles.IsNullOrEmpty() && team.guestPlayerProfiles.IsNullOrEmpty()))
        {
            return "Each team must have at least one player";
        }

        return "";
    }

    private string GetRoundsConfigErrorMessage()
    {
        if (PartyModeSettings.roundCount <= 0)
        {
            return "Must play at least one round";
        }

        if (PartyModeSettings.teamSettings.isKnockOutTournament)
        {
            if (PartyModeSettings.teamSettings.isFreeForAll)
            {
                // N players => at most (N - 1) rounds to play, until there is a single winning player.
                int playerCount = PartyModeSettings.teamSettings.teams
                    .Select(team => team.playerProfiles.Count + team.guestPlayerProfiles.Count)
                    .Sum();
                if (PartyModeSettings.roundCount >= playerCount)
                {
                    return "Too many rounds for knock-out tournament";
                }
            }
            else
            {
                // N teams => at most (N - 1) rounds to play, until there is a single winning team.
                int teamCount = PartyModeSettings.teamSettings.teams.Count;
                if (PartyModeSettings.roundCount >= teamCount)
                {
                    return "Too many rounds for knock-out tournament";
                }
            }
        }

        return "";
    }

    private void AddPlayerProfilesToTeams(bool guests)
    {
        PartyModeSettings.teamSettings.teams.ForEach(teamList =>
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
            .Union(PartyModeSettings.guestPlayerProfiles)
            .ToList();
        if (allRelevantPlayerProfiles.IsNullOrEmpty())
        {
            PartyModeSettings.teamSettings.teams.ForEach(team =>
            {
                // No players to assign
                team.playerProfiles.Clear();
                team.guestPlayerProfiles.Clear();
            });
            return;
        }

        List<PlayerProfile> playerProfiles = guests
            ? PartyModeSettings.guestPlayerProfiles
            : settings.PlayerProfiles;
        List<PlayerProfile> relevantPlayerProfiles = playerProfiles
            .Where(playerProfile => playerProfile.IsEnabled)
            .ToList();
        List<PlayerProfile> assignedPlayerProfiles = PartyModeSettings.teamSettings.teams
            .SelectMany(team => team.playerProfiles.Union(team.guestPlayerProfiles))
            .ToList();
        List<PlayerProfile> relevantUnassignedPlayerProfiles = relevantPlayerProfiles
            .Where(playerProfile => !assignedPlayerProfiles.Contains(playerProfile))
            .ToList();
        foreach (PlayerProfile playerProfile in relevantUnassignedPlayerProfiles)
        {
            int playerProfileIndex = allRelevantPlayerProfiles.IndexOf(playerProfile);
            double playerProfilePercent = playerProfileIndex / (double)(allRelevantPlayerProfiles.Count - 1);
            int teamIndex = (int)Math.Round(playerProfilePercent * (PartyModeSettings.teamSettings.teams.Count - 1));
            teamIndex = NumberUtils.Limit(teamIndex, 0, PartyModeSettings.teamSettings.teams.Count - 1);
            PartyModeTeamSettings team = PartyModeSettings.teamSettings.teams[teamIndex];
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
