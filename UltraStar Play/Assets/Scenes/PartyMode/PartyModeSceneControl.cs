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
    private UiManager uiManager;

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

    private readonly PartyModeSettings partyModeSettings = new();
    private readonly ReactiveProperty<EPartyModeConfigPart> configPart = new(EPartyModeConfigPart.Teams);
    private readonly PartyModeTeamConfigControl teamConfigControl = new();
    private readonly PartyModeSongSelectionConfigControl songSelectionConfigControl = new();
    private readonly PartyModeRoundsConfigControl roundsConfigControl = new();

    public void OnInjectionFinished()
    {
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
        for (int i = partyModeSettings.TeamSettings.Teams.Count; i < 2; i++)
        {
            PartyModeTeamSettings newTeam = new();
            newTeam.Name = PartyModeTeamConfigControl.GetDefaultTeamName(newTeam, partyModeSettings);
            partyModeSettings.TeamSettings.Teams.Add(newTeam);
        }

        // Add all players that are selected for singing to the teams.
        // First, add the regular player profiles. Afterwards, add the guest profiles.
        AddPlayerProfilesToTeams(false);
        AddPlayerProfilesToTeams(true);

        // Add at least one round
        if (partyModeSettings.RoundsSettings.GameRoundSettings.IsNullOrEmpty())
        {
            partyModeSettings.RoundsSettings.GameRoundSettings.Add(new GameRoundSettings());
        }
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
        if (partyModeSettings.SongSelectionSettings.SongPoolPlaylist == null
            || partyModeSettings.SongSelectionSettings.SongPoolPlaylist.IsEmpty)
        {
            return "Select a playlist that is not empty";
        }

        return "";
    }

    private string GetTeamsConfigErrorMessage()
    {
        if (partyModeSettings.TeamSettings.Teams.Count < 2)
        {
            return "Must use at least two teams";
        }

        if (partyModeSettings.TeamSettings.Teams
            .AnyMatch(team => team.PlayerProfiles.IsNullOrEmpty() && team.GuestPlayerProfiles.IsNullOrEmpty()))
        {
            return "Each team must have at least one player";
        }

        return "";
    }

    private string GetRoundsConfigErrorMessage()
    {
        if (partyModeSettings.RoundsSettings.GameRoundSettings.Count <= 0)
        {
            return "Must play at least one round";
        }

        return "";
    }

    private void AddPlayerProfilesToTeams(bool guests)
    {
        partyModeSettings.TeamSettings.Teams.ForEach(teamList =>
        {
            if (guests)
            {
                teamList.GuestPlayerProfiles.Clear();
            }
            else
            {
                teamList.PlayerProfiles.Clear();
            }
        });

        List<PlayerProfile> allPlayerProfiles = settings.PlayerProfiles.Union(settings.GuestPlayerProfiles).ToList();
        List<PlayerProfile> playerProfiles = guests
            ? settings.GuestPlayerProfiles
            : settings.PlayerProfiles;
        List<PlayerProfile> relevantPlayerProfiles = playerProfiles
            .Where(playerProfile => playerProfile.IsEnabled)
            .ToList();
        List<PlayerProfile> assignedPlayerProfiles = partyModeSettings.TeamSettings.Teams
            .SelectMany(team => team.GuestPlayerProfiles.Union(team.PlayerProfiles))
            .ToList();
        List<PlayerProfile> relevantUnassignedPlayerProfiles = relevantPlayerProfiles
            .Where(playerProfile => !assignedPlayerProfiles.Contains(playerProfile))
            .ToList();
        foreach (PlayerProfile playerProfile in relevantUnassignedPlayerProfiles)
        {
            int playerProfileIndex = allPlayerProfiles.IndexOf(playerProfile);
            int teamIndex = playerProfileIndex < Math.Ceiling((double)allPlayerProfiles.Count / 2)
                ? 0
                : 1;
            PartyModeTeamSettings team = partyModeSettings.TeamSettings.Teams[teamIndex];
            List<PlayerProfile> targetList = guests ? team.GuestPlayerProfiles : team.PlayerProfiles;
            targetList.Add(playerProfile);
        }
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new();
        bb.BindExistingInstance(gameObject);
        bb.BindExistingInstance(this);
        bb.BindExistingInstance(partyModeSettings);
        bb.BindExistingInstance(teamConfigControl);
        bb.BindExistingInstance(songSelectionConfigControl);
        bb.BindExistingInstance(roundsConfigControl);
        bb.Bind(nameof(teamColumnUi)).ToExistingInstance(teamColumnUi);
        bb.Bind(nameof(teamColumnPlayerUi)).ToExistingInstance(teamColumnPlayerUi);
        bb.Bind(nameof(roundUi)).ToExistingInstance(roundUi);
        return bb.GetBindings();
    }
}
