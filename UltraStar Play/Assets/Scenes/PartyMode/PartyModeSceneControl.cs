using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;
using UnityEngine.PlayerLoop;
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

    [InjectedInInspector]
    public VisualTreeAsset roundUi;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private Injector injector;

    [Inject]
    private Settings settings;

    [Inject]
    private PartyModeSettings partyModeSettings;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private PlaylistManager playlistManager;

    [Inject(UxmlName = R.UxmlNames.partyModeTeamConfigUi)]
    private VisualElement partyModeTeamConfigUi;

    [Inject(UxmlName = R.UxmlNames.partyModeSongSelectionConfigUi)]
    private VisualElement partyModeSongSelectionConfigUi;

    [Inject(UxmlName = R.UxmlNames.partyModeRoundConfigUi)]
    private VisualElement partyModeRoundConfigUi;

    [Inject(UxmlName = R.UxmlNames.backButton)]
    private Button backButton;

    [Inject(UxmlName = R.UxmlNames.continueButton)]
    private Button continueButton;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    private readonly ReactiveProperty<EPartyModeConfigPart> configPart = new(EPartyModeConfigPart.Teams);
    private readonly PartyModeTeamConfigControl teamConfigControl = new();
    private readonly PartyModeSongSelectionConfigControl songSelectionConfigControl = new();
    private readonly PartyModeRoundsConfigControl roundsConfigControl = new();

    public void OnInjectionFinished()
    {
        songMetaManager.ScanFilesIfNotDoneYet();

        InitPartyModeSettings();

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable()
            .Subscribe(_ => OnBack());

        backButton.RegisterCallbackButtonTriggered(() => OnBack());
        continueButton.RegisterCallbackButtonTriggered(() => OnContinue());

        configPart.Subscribe(_ => UpdateConfigPart());
        UpdateConfigPart();

        // Inject child controls
        injector.Inject(teamConfigControl);
        injector.Inject(songSelectionConfigControl);
        injector.Inject(roundsConfigControl);
    }

    private void InitPartyModeSettings()
    {
        // Add at least two teams
        for (int i = partyModeSettings.teamSettings.teams.Count; i < 2; i++)
        {
            PartyModeTeamSettings newTeam = new();
            newTeam.name = PartyModeTeamConfigControl.GetDefaultTeamName(newTeam, partyModeSettings);
            partyModeSettings.teamSettings.teams.Add(newTeam);
        }

        // Add all players that are selected for singing to the teams.
        // First, add the regular player profiles. Afterwards, add the guest profiles.
        AddPlayerProfilesToTeams(false);
        AddPlayerProfilesToTeams(true);

        // Add at least one round
        if (partyModeSettings.roundsSettings.gameRoundSettings.IsNullOrEmpty())
        {
            partyModeSettings.roundsSettings.gameRoundSettings.Add(new GameRoundSettings());
        }

        // Select the "all songs" playlist
        partyModeSettings.songSelectionSettings.songPoolPlaylist = UltraStarAllSongsPlaylist.Instance;
    }

    private void UpdateConfigPart()
    {
        VisualElement GetCurrentConfigPartVisualElement()
        {
            switch (configPart.Value)
            {
                case EPartyModeConfigPart.Teams:
                    return partyModeTeamConfigUi;
                case EPartyModeConfigPart.SongSelection:
                    return partyModeSongSelectionConfigUi;
                case EPartyModeConfigPart.Rounds:
                    return partyModeRoundConfigUi;
            }

            throw new ArgumentException($"Unhandled config part {configPart.Value}");
        }

        // Show only the current config part UI
        List<VisualElement> configUis = new List<VisualElement>
        {
            partyModeTeamConfigUi,
            partyModeSongSelectionConfigUi,
            partyModeRoundConfigUi,
        };
        configUis.ForEach(configUi => configUi.HideByDisplay());
        GetCurrentConfigPartVisualElement().ShowByDisplay();

        // Update the scene title
        sceneTitle.text = $"Party Mode - {StringUtils.ToTitleCase(configPart.Value.ToString())}";
    }

    private void OnBack()
    {
        if (configPart.Value == EPartyModeConfigPart.Teams)
        {
            sceneNavigator.LoadScene(EScene.MainScene);
        }
        else if (configPart.Value == EPartyModeConfigPart.SongSelection)
        {
            configPart.Value = EPartyModeConfigPart.Teams;
        }
        else if (configPart.Value == EPartyModeConfigPart.Rounds)
        {
            configPart.Value = EPartyModeConfigPart.SongSelection;
        }
	}

    private void OnContinue()
    {
        if (configPart.Value == EPartyModeConfigPart.Teams)
        {
            string errorMessage = GetTeamsConfigErrorMessage();
            if (!errorMessage.IsNullOrEmpty())
            {
                uiManager.CreateNotificationVisualElement(errorMessage);
                return;
            }

            configPart.Value = EPartyModeConfigPart.SongSelection;
        }
        else if (configPart.Value == EPartyModeConfigPart.SongSelection)
        {
            string errorMessage = GetSongSelectionConfigErrorMessage();
            if (!errorMessage.IsNullOrEmpty())
            {
                uiManager.CreateNotificationVisualElement(errorMessage);
                return;
            }

            configPart.Value = EPartyModeConfigPart.Rounds;
        }
        else if (configPart.Value == EPartyModeConfigPart.Rounds)
        {
            string errorMessage = GetRoundsConfigErrorMessage();
            if (!errorMessage.IsNullOrEmpty())
            {
                uiManager.CreateNotificationVisualElement(errorMessage);
                return;
            }

            // All config done, start the first party round
            SongSelectSceneData songSelectSceneData = new();
            songSelectSceneData.PartyModeSettings = partyModeSettings;
            sceneNavigator.LoadScene(EScene.SongSelectScene, songSelectSceneData);
        }
    }

    private string GetSongSelectionConfigErrorMessage()
    {
        if (partyModeSettings.songSelectionSettings.songPoolPlaylist == null
            || partyModeSettings.songSelectionSettings.songPoolPlaylist.IsEmpty)
        {
            return "Select a playlist that is not empty";
        }

        return "";
    }

    private string GetTeamsConfigErrorMessage()
    {
        if (partyModeSettings.teamSettings.teams.Count < 1)
        {
            return "Must use at least one teams";
        }

        if (partyModeSettings.teamSettings.teams
            .AnyMatch(team => team.playerProfiles.IsNullOrEmpty() && team.guestPlayerProfiles.IsNullOrEmpty()))
        {
            return "Each team must have at least one player";
        }

        return "";
    }

    private string GetRoundsConfigErrorMessage()
    {
        if (partyModeSettings.roundsSettings.gameRoundSettings.Count <= 0)
        {
            return "Must play at least one round";
        }

        if (partyModeSettings.teamSettings.isKnockOutTournament)
        {
            if (partyModeSettings.teamSettings.isFreeForAll)
            {
                // N players => at most (N - 1) rounds to play, until there is a single winning player.
                int playerCount = partyModeSettings.teamSettings.teams
                    .Select(team => team.playerProfiles.Count + team.guestPlayerProfiles.Count)
                    .Sum();
                if (partyModeSettings.roundsSettings.gameRoundSettings.Count >= playerCount)
                {
                    return "Too many rounds for knock-out tournament";
                }
            }
            else
            {
                // N teams => at most (N - 1) rounds to play, until there is a single winning team.
                int teamCount = partyModeSettings.teamSettings.teams.Count;
                if (partyModeSettings.roundsSettings.gameRoundSettings.Count >= teamCount)
                {
                    return "Too many rounds for knock-out tournament";
                }
            }
        }

        return "";
    }

    private void AddPlayerProfilesToTeams(bool guests)
    {
        partyModeSettings.teamSettings.teams.ForEach(teamList =>
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
            .Union(partyModeSettings.guestPlayerProfiles)
            .ToList();
        if (allRelevantPlayerProfiles.IsNullOrEmpty())
        {
            partyModeSettings.teamSettings.teams.ForEach(team =>
            {
                // No players to assign
                team.playerProfiles.Clear();
                team.guestPlayerProfiles.Clear();
            });
            return;
        }

        List<PlayerProfile> playerProfiles = guests
            ? partyModeSettings.guestPlayerProfiles
            : settings.PlayerProfiles;
        List<PlayerProfile> relevantPlayerProfiles = playerProfiles
            .Where(playerProfile => playerProfile.IsEnabled)
            .ToList();
        List<PlayerProfile> assignedPlayerProfiles = partyModeSettings.teamSettings.teams
            .SelectMany(team => team.playerProfiles.Union(team.guestPlayerProfiles))
            .ToList();
        List<PlayerProfile> relevantUnassignedPlayerProfiles = relevantPlayerProfiles
            .Where(playerProfile => !assignedPlayerProfiles.Contains(playerProfile))
            .ToList();
        foreach (PlayerProfile playerProfile in relevantUnassignedPlayerProfiles)
        {
            int playerProfileIndex = allRelevantPlayerProfiles.IndexOf(playerProfile);
            double playerProfilePercent = playerProfileIndex / (double)(allRelevantPlayerProfiles.Count - 1);
            int teamIndex = (int)Math.Round(playerProfilePercent * (partyModeSettings.teamSettings.teams.Count - 1));
            teamIndex = NumberUtils.Limit(teamIndex, 0, partyModeSettings.teamSettings.teams.Count - 1);
            PartyModeTeamSettings team = partyModeSettings.teamSettings.teams[teamIndex];
            List<PlayerProfile> targetList = guests ? team.guestPlayerProfiles : team.playerProfiles;
            targetList.Add(playerProfile);
        }
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new();
        bb.BindExistingInstance(gameObject);
        bb.BindExistingInstance(this);
        bb.BindExistingInstance(teamConfigControl);
        bb.BindExistingInstance(songSelectionConfigControl);
        bb.BindExistingInstance(roundsConfigControl);
        bb.Bind(nameof(valueInputDialogUi)).ToExistingInstance(valueInputDialogUi);
        bb.Bind(nameof(teamColumnUi)).ToExistingInstance(teamColumnUi);
        bb.Bind(nameof(teamColumnPlayerUi)).ToExistingInstance(teamColumnPlayerUi);
        bb.Bind(nameof(roundUi)).ToExistingInstance(roundUi);
        return bb.GetBindings();
    }
}
