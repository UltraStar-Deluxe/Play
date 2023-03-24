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

    [Inject(UxmlName = R.UxmlNames.teamList)]
    private VisualElement teamList;

    [Inject(UxmlName = R.UxmlNames.freeForAllItemPicker)]
    private ItemPicker freeForAllItemPicker;

    [Inject(UxmlName = R.UxmlNames.knockOutTournamentItemPicker)]
    private ItemPicker knockOutTournamentItemPicker;

    [Inject(UxmlName = R.UxmlNames.addTeamButton)]
    private Button addTeamButton;

    [Inject(UxmlName = R.UxmlNames.addGuestButton)]
    private Button addGuestButton;

    [Inject(UxmlName = R.UxmlNames.teamColumnsContainer)]
    private VisualElement teamColumnsContainer;
    
    [Inject(UxmlName = R.UxmlNames.teamConfigUiRoot)]
    private VisualElement teamConfigUiRoot;

    private Dictionary<PartyModeTeamSettings, VisualElement> teamToVisualElement = new();
    private Dictionary<PlayerProfile, VisualElement> playerToVisualElement = new();

    public void OnInjectionFinished()
    {
        new BoolPickerControl(freeForAllItemPicker)
            .Bind(() => partyModeSettings.teamSettings.isFreeForAll,
                newValue => partyModeSettings.teamSettings.isFreeForAll = newValue);

        new BoolPickerControl(knockOutTournamentItemPicker)
            .Bind(() => partyModeSettings.teamSettings.isKnockOutTournament, 
                newValue => partyModeSettings.teamSettings.isKnockOutTournament = newValue);

        partyModeSettings.ObserveEveryValueChanged(it => it.teamSettings.isFreeForAll)
            .Subscribe(_ => UpdateTeams());

        addTeamButton.RegisterCallbackButtonTriggered(_ => AddTeam());
        addGuestButton.RegisterCallbackButtonTriggered(_ => AddGuest());
        UpdateTeams();
    }

    private void AddTeam()
    {
        if (partyModeSettings.teamSettings.teams.IsNullOrEmpty())
        {
            partyModeSettings.teamSettings.teams = new();
        }

        PartyModeTeamSettings newTeam = new();

        partyModeSettings.teamSettings.teams.Add(newTeam);
        newTeam.name = GetDefaultTeamName(newTeam, partyModeSettings);
        UpdateTeams();
    }

    private void AddGuest()
    {
        if (partyModeSettings.teamSettings.teams.IsNullOrEmpty())
        {
            AddTeam();
        }

        PlayerProfile newGuestProfile = new("", EDifficulty.Medium);
        string newGuestProfileName = GetDefaultGuestProfileName(newGuestProfile, partyModeSettings);
        newGuestProfile.Name = newGuestProfileName;
        partyModeSettings.teamSettings.teams.FirstOrDefault().guestPlayerProfiles.Add(newGuestProfile);
        partyModeSettings.guestPlayerProfiles.Add(newGuestProfile);

        UpdateTeams();
    }

    private void UpdateTeams()
    {
        teamConfigUiRoot.SetVisibleByDisplay(!partyModeSettings.teamSettings.isFreeForAll);
        teamColumnsContainer.Clear();
        teamToVisualElement.Clear();
        playerToVisualElement.Clear();

        partyModeSettings.teamSettings.teams.ForEach(team => CreateTeamUi(team));
        
        ThemeManager.ApplyThemeSpecificStylesToVisualElements(teamList);
    }

    private void CreateTeamUi(PartyModeTeamSettings team)
    {
        VisualElement teamVisualElement = teamColumnUi.CloneTree().Children().FirstOrDefault();
        teamToVisualElement[team] = teamVisualElement;
        teamColumnsContainer.Add(teamVisualElement);

        // Edit team name
        TextField teamNameTextField = teamVisualElement.Q<TextField>(R.UxmlNames.teamNameTextField);
        FieldBindingUtils.Bind(gameObject, teamNameTextField,
            () => team.name,
            newValue => team.name = newValue);
        FieldBindingUtils.ResetValueOnBlurIfEmpty(teamNameTextField);

        // Delete team
        Button deleteTeamButton = teamVisualElement.Q<Button>(R.UxmlNames.deleteTeamButton);
        deleteTeamButton.RegisterCallbackButtonTriggered(_ => DeleteTeam(team));
        deleteTeamButton.SetEnabled(partyModeSettings.teamSettings.teams.Count > 1);

        // Sort player profiles
        SortPlayerProfiles(team);

        // Add players to team
        VisualElement playersContainer = teamVisualElement.Q<VisualElement>(R.UxmlNames.playersContainer);
        playersContainer.Clear();
        team.playerProfiles.ForEach(playerProfile => CreateTeamPlayerUi(team, playerProfile, false));
        team.guestPlayerProfiles.ForEach(playerProfile => CreateTeamPlayerUi(team, playerProfile, true));
    }

    private void SortPlayerProfiles(PartyModeTeamSettings team)
    {
        List<PlayerProfile> allPlayerProfiles = GetAllPlayerProfiles();
        Comparison<PlayerProfile> comparerByIndexInAllPlayerProfiles = new Comparison<PlayerProfile>((a, b) => allPlayerProfiles.IndexOf(a).CompareTo(allPlayerProfiles.IndexOf(b)));
        team.playerProfiles.Sort(comparerByIndexInAllPlayerProfiles);
        team.guestPlayerProfiles.Sort(comparerByIndexInAllPlayerProfiles);
    }

    private void DeleteTeam(PartyModeTeamSettings team)
    {
        if (partyModeSettings.teamSettings.teams.Count <= 1)
        {
            // There should be at least one team
            return;
        }

        // Move players to other team
        PartyModeTeamSettings otherTeam = partyModeSettings.teamSettings.teams.GetElementBefore(team, false);
        if (otherTeam == null)
        {
            otherTeam = partyModeSettings.teamSettings.teams.GetElementAfter(team, false);
        }
        otherTeam.playerProfiles.AddRange(team.playerProfiles);
        otherTeam.guestPlayerProfiles.AddRange(team.guestPlayerProfiles);

        partyModeSettings.teamSettings.teams.Remove(team);
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
            deleteGuestButton.RegisterCallbackButtonTriggered(_ => DeleteGuestPlayer(team, playerProfile, playerVisualElement));
        }
        else
        {
            playerNameLabel.text = playerProfile.Name;
        }

        // Move player to other team
        Button leftButton = playerVisualElement.Q<Button>(R.UxmlNames.leftButton);
        Button rightButton = playerVisualElement.Q<Button>(R.UxmlNames.rightButton);

        leftButton.RegisterCallbackButtonTriggered(_ =>
        {
            MovePlayerToLeftTeam(team, playerProfile, isGuest);
            // Focus new button
            playerToVisualElement[playerProfile]?.Q<Button>(R.UxmlNames.leftButton)?.Focus();

        });
        rightButton.RegisterCallbackButtonTriggered(_ =>
        {
            MovePlayerToRightTeam(team, playerProfile, isGuest);
            // Focus new button
            playerToVisualElement[playerProfile]?.Q<Button>(R.UxmlNames.rightButton)?.Focus();
            
        });
    }

    private void MovePlayerToRightTeam(PartyModeTeamSettings team, PlayerProfile playerProfile, bool isGuest)
    {
        PartyModeTeamSettings rightTeam = partyModeSettings.teamSettings.teams.GetElementAfter(team, false);
        if (rightTeam != null)
        {
            GetPlayerProfileList(team, isGuest).Remove(playerProfile);
            GetPlayerProfileList(rightTeam, isGuest).Add(playerProfile);
            UpdateTeams();
        }
    }

    private void MovePlayerToLeftTeam(PartyModeTeamSettings team, PlayerProfile playerProfile, bool isGuest)
    {
        PartyModeTeamSettings leftTeam = partyModeSettings.teamSettings.teams.GetElementBefore(team, false);
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
        partyModeSettings.guestPlayerProfiles.Remove(playerProfile);
    }

    private List<PlayerProfile> GetAllPlayerProfiles()
    {
        return settings.PlayerProfiles
            .Where(playerProfile => playerProfile.IsEnabled)
            .Union(partyModeSettings.guestPlayerProfiles).ToList();
    }

    public static string GetDefaultTeamName(PartyModeTeamSettings team, PartyModeSettings partyModeSettings)
    {
        int teamIndex = partyModeSettings.teamSettings.teams.IndexOf(team);
        if (teamIndex < 0)
        {
            teamIndex = partyModeSettings.teamSettings.teams.Count;
        }
        string newTeamNumber = StringUtils.AddLeadingZeros(teamIndex + 1, 2);
        return $"Team {newTeamNumber}";
    }

    private static string GetDefaultGuestProfileName(PlayerProfile playerProfile, PartyModeSettings partyModeSettings)
    {
        List<PlayerProfile> allGuestProfiles =
            partyModeSettings.teamSettings.teams
                .SelectMany(team => team.guestPlayerProfiles)
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
            ? team.guestPlayerProfiles
            : team.playerProfiles;
    }
}
