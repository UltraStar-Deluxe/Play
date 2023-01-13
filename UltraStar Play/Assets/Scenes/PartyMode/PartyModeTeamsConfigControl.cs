using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PartyModeTeamConfigControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Key = nameof(teamColumnUi))]
    private VisualTreeAsset teamColumnUi;

    [Inject(Key = nameof(teamColumnPlayerUi))]
    private VisualTreeAsset teamColumnPlayerUi;

    [Inject]
    private Settings settings;

    [Inject]
    private PartyModeSettings partyModeSettings;

    [Inject]
    private GameObject gameObject;

    [Inject(UxmlName = R.UxmlNames.teamsScrollView)]
    private VisualElement teamsScrollView;

    [Inject(UxmlName = R.UxmlNames.freeForAllToggle)]
    private Toggle freeForAllToggle;

    [Inject(UxmlName = R.UxmlNames.knockOutTournamentToggle)]
    private Toggle knockOutTournamentToggle;

    [Inject(UxmlName = R.UxmlNames.addTeamButton)]
    private Button addTeamButton;

    [Inject(UxmlName = R.UxmlNames.addGuestButton)]
    private Button addGuestButton;

    [Inject(UxmlName = R.UxmlNames.teamColumnsContainer)]
    private VisualElement teamColumnsContainer;

    private Dictionary<PartyModeTeamSettings, VisualElement> teamToVisualElement = new();
    private Dictionary<PlayerProfile, VisualElement> playerToVisualElement = new();

    public void OnInjectionFinished()
    {
        FieldBindingUtils.Bind(gameObject, freeForAllToggle,
            () => partyModeSettings.TeamSettings.IsFreeForAll,
            newValue => partyModeSettings.TeamSettings.IsFreeForAll = newValue);

        FieldBindingUtils.Bind(gameObject, knockOutTournamentToggle,
            () => partyModeSettings.TeamSettings.IsKnockOutTournament,
            newValue => partyModeSettings.TeamSettings.IsKnockOutTournament = newValue);

        partyModeSettings.ObserveEveryValueChanged(it => it.TeamSettings.IsFreeForAll)
            .Subscribe(_ => UpdateTeams());

        addTeamButton.RegisterCallbackButtonTriggered(() => AddTeam());
        addGuestButton.RegisterCallbackButtonTriggered(() => AddGuest());
        UpdateTeams();
    }

    private void AddTeam()
    {
        if (partyModeSettings.TeamSettings.Teams.IsNullOrEmpty())
        {
            partyModeSettings.TeamSettings.Teams = new();
        }

        PartyModeTeamSettings newTeam = new();

        partyModeSettings.TeamSettings.Teams.Add(newTeam);
        newTeam.Name = GetDefaultTeamName(newTeam, partyModeSettings);
        UpdateTeams();
    }

    private void AddGuest()
    {
        if (partyModeSettings.TeamSettings.Teams.IsNullOrEmpty())
        {
            AddTeam();
        }

        PlayerProfile newGuestProfile = new("", EDifficulty.Medium, EAvatar.GenericPlayer01);
        string newGuestProfileName = GetDefaultGuestProfileName(newGuestProfile, partyModeSettings);
        newGuestProfile.Name = newGuestProfileName;
        partyModeSettings.TeamSettings.Teams.FirstOrDefault().GuestPlayerProfiles.Add(newGuestProfile);
        partyModeSettings.GuestPlayerProfiles.Add(newGuestProfile);

        UpdateTeams();
    }

    private void UpdateTeams()
    {
        teamsScrollView.SetVisibleByDisplay(!partyModeSettings.TeamSettings.IsFreeForAll);
        teamColumnsContainer.Clear();
        teamToVisualElement.Clear();
        playerToVisualElement.Clear();

        partyModeSettings.TeamSettings.Teams.ForEach(team => CreateTeamUi(team));
    }

    private void CreateTeamUi(PartyModeTeamSettings team)
    {
        VisualElement teamVisualElement = teamColumnUi.CloneTree().Children().FirstOrDefault();
        teamToVisualElement[team] = teamVisualElement;
        teamColumnsContainer.Add(teamVisualElement);

        // Edit team name
        TextField teamNameTextField = teamVisualElement.Q<TextField>(R.UxmlNames.teamNameTextField);
        FieldBindingUtils.Bind(gameObject, teamNameTextField,
            () => team.Name,
            newValue => team.Name = newValue);
        FieldBindingUtils.ResetValueOnBlurIfEmpty(teamNameTextField);

        // Delete team
        Button deleteTeamButton = teamVisualElement.Q<Button>(R.UxmlNames.deleteTeamButton);
        deleteTeamButton.RegisterCallbackButtonTriggered(() => DeleteTeam(team));
        deleteTeamButton.SetEnabled(partyModeSettings.TeamSettings.Teams.Count > 1);

        // Sort player profiles
        SortPlayerProfiles(team);

        // Add players to team
        VisualElement playersContainer = teamVisualElement.Q<VisualElement>(R.UxmlNames.playersContainer);
        playersContainer.Clear();
        team.PlayerProfiles.ForEach(playerProfile => CreateTeamPlayerUi(team, playerProfile, false));
        team.GuestPlayerProfiles.ForEach(playerProfile => CreateTeamPlayerUi(team, playerProfile, true));
    }

    private void SortPlayerProfiles(PartyModeTeamSettings team)
    {
        List<PlayerProfile> allPlayerProfiles = GetAllPlayerProfiles();
        Comparison<PlayerProfile> comparerByIndexInAllPlayerProfiles = new Comparison<PlayerProfile>((a, b) => allPlayerProfiles.IndexOf(a).CompareTo(allPlayerProfiles.IndexOf(b)));
        team.PlayerProfiles.Sort(comparerByIndexInAllPlayerProfiles);
        team.GuestPlayerProfiles.Sort(comparerByIndexInAllPlayerProfiles);
    }

    private void DeleteTeam(PartyModeTeamSettings team)
    {
        if (partyModeSettings.TeamSettings.Teams.Count <= 1)
        {
            // There should be at least one team
            return;
        }

        // Move players to other team
        PartyModeTeamSettings otherTeam = partyModeSettings.TeamSettings.Teams.GetElementBefore(team, false);
        if (otherTeam == null)
        {
            otherTeam = partyModeSettings.TeamSettings.Teams.GetElementAfter(team, false);
        }
        otherTeam.PlayerProfiles.AddRange(team.PlayerProfiles);
        otherTeam.GuestPlayerProfiles.AddRange(team.GuestPlayerProfiles);

        partyModeSettings.TeamSettings.Teams.Remove(team);
        UpdateTeams();
    }

    private void CreateTeamPlayerUi(PartyModeTeamSettings team, PlayerProfile playerProfile, bool isGuest)
    {
        VisualElement playerVisualElement = teamColumnPlayerUi.CloneTree().Children().FirstOrDefault();
        playerToVisualElement[playerProfile] = playerVisualElement;

        VisualElement teamVisualElement = teamToVisualElement[team];
        VisualElement playersContainer = teamVisualElement.Q<VisualElement>(R.UxmlNames.playersContainer);

        AddPlaceholdersBeforePlayerProfile(team, playerProfile, isGuest, playersContainer);
        playersContainer.Add(playerVisualElement);

        Label playerNameLabel = playerVisualElement.Q<Label>(R.UxmlNames.playerNameLabel);
        TextField guestNameTextField = playerVisualElement.Q<TextField>(R.UxmlNames.guestNameTextField);
        Button deleteGuestButton = playerVisualElement.Q<Button>(R.UxmlNames.deleteGuestButton);

        playerNameLabel.SetVisibleByDisplay(!isGuest);
        guestNameTextField.SetVisibleByDisplay(isGuest);
        deleteGuestButton.SetVisibleByDisplay(isGuest);
        if (isGuest)
        {
            guestNameTextField.value = playerProfile.Name;
            FieldBindingUtils.Bind(gameObject, guestNameTextField,
                () => playerProfile.Name,
                newValue => playerProfile.Name = newValue);
            FieldBindingUtils.ResetValueOnBlurIfEmpty(guestNameTextField);
            deleteGuestButton.RegisterCallbackButtonTriggered(() => DeleteGuestPlayer(team, playerProfile, playerVisualElement));
        }
        else
        {
            playerNameLabel.text = playerProfile.Name;
        }

        // Move player to other team
        Button leftButton = playerVisualElement.Q<Button>(R.UxmlNames.leftButton);
        Button rightButton = playerVisualElement.Q<Button>(R.UxmlNames.rightButton);

        leftButton.RegisterCallbackButtonTriggered(() => MovePlayerToLeftTeam(team, playerProfile, isGuest));
        rightButton.RegisterCallbackButtonTriggered(() => MovePlayerToRightTeam(team, playerProfile, isGuest));
    }

    private void MovePlayerToRightTeam(PartyModeTeamSettings team, PlayerProfile playerProfile, bool isGuest)
    {
        PartyModeTeamSettings rightTeam = partyModeSettings.TeamSettings.Teams.GetElementAfter(team, false);
        if (rightTeam != null)
        {
            GetPlayerProfileList(team, isGuest).Remove(playerProfile);
            GetPlayerProfileList(rightTeam, isGuest).Add(playerProfile);
            UpdateTeams();
        }
    }

    private void MovePlayerToLeftTeam(PartyModeTeamSettings team, PlayerProfile playerProfile, bool isGuest)
    {
        PartyModeTeamSettings leftTeam = partyModeSettings.TeamSettings.Teams.GetElementBefore(team, false);
        if (leftTeam != null)
        {
            GetPlayerProfileList(team, isGuest).Remove(playerProfile);
            GetPlayerProfileList(leftTeam, isGuest).Add(playerProfile);
            UpdateTeams();
        }
    }

    private void AddPlaceholdersBeforePlayerProfile(PartyModeTeamSettings currentTeam, PlayerProfile currentPlayerProfile, bool isGuest, VisualElement targetVisualElement)
    {
        // Each player profile must be on its own row in the UI.
        // Therefor, placeholders are added until the player profile is displayed on the correct row.
        int playerProfileIndex = GetAllPlayerProfiles().IndexOf(currentPlayerProfile);
        int takenSlotsCount = targetVisualElement.childCount;
        int slotCountToBeFilled = playerProfileIndex - takenSlotsCount;
        for (int i = 0; i < slotCountToBeFilled; i++)
        {
            VisualElement placeholderVisualElement = new();
            placeholderVisualElement.AddToClassList("playerProfilePlaceholder");
            targetVisualElement.Add(placeholderVisualElement);
        }
    }

    private void DeleteGuestPlayer(PartyModeTeamSettings team, PlayerProfile playerProfile, VisualElement playerVisualElement)
    {
        playerVisualElement.RemoveFromHierarchy();
        playerToVisualElement.Remove(playerProfile);
        partyModeSettings.GuestPlayerProfiles.Remove(playerProfile);
    }

    private List<PlayerProfile> GetAllPlayerProfiles()
    {
        return settings.PlayerProfiles
            .Where(playerProfile => playerProfile.IsEnabled)
            .Union(partyModeSettings.GuestPlayerProfiles).ToList();
    }

    public static string GetDefaultTeamName(PartyModeTeamSettings team, PartyModeSettings partyModeSettings)
    {
        int teamIndex = partyModeSettings.TeamSettings.Teams.IndexOf(team);
        if (teamIndex < 0)
        {
            teamIndex = partyModeSettings.TeamSettings.Teams.Count;
        }
        string newTeamNumber = StringUtils.AddLeadingZeros(teamIndex + 1, 2);
        return $"Team {newTeamNumber}";
    }

    private static string GetDefaultGuestProfileName(PlayerProfile playerProfile, PartyModeSettings partyModeSettings)
    {
        List<PlayerProfile> allGuestProfiles =
            partyModeSettings.TeamSettings.Teams
                .SelectMany(team => team.GuestPlayerProfiles)
                .ToList();
        int guestIndex = allGuestProfiles.IndexOf(playerProfile);
        if (guestIndex < 0)
        {
            guestIndex = allGuestProfiles.Count;
        }
        string newGuestNumber = StringUtils.AddLeadingZeros(guestIndex + 1, 2);
        return $"Guest {newGuestNumber}";
    }

    private static List<PlayerProfile> GetPlayerProfileList(PartyModeTeamSettings team, bool isGuest)
    {
        return isGuest
            ? team.GuestPlayerProfiles
            : team.PlayerProfiles;
    }
}
