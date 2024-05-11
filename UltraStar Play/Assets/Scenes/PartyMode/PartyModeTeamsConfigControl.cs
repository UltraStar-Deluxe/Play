using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

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

    [Inject(UxmlName = R.UxmlNames.freeForAllItemToggle)]
    private Toggle freeForAllItemToggle;

    [Inject(UxmlName = R.UxmlNames.knockOutTournamentItemToggle)]
    private Toggle knockOutTournamentItemToggle;

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
        FieldBindingUtils.Bind(freeForAllItemToggle,
            () => partyModeSettings.TeamSettings.IsFreeForAll,
            newValue => partyModeSettings.TeamSettings.IsFreeForAll = newValue);

        FieldBindingUtils.Bind(knockOutTournamentItemToggle,
            () => partyModeSettings.TeamSettings.IsKnockOutTournament,
            newValue => partyModeSettings.TeamSettings.IsKnockOutTournament = newValue);

        partyModeSettings.ObserveEveryValueChanged(it => it.TeamSettings.IsFreeForAll)
            .Subscribe(_ => UpdateTeams());

        addTeamButton.RegisterCallbackButtonTriggered(_ => AddTeam());
        addGuestButton.RegisterCallbackButtonTriggered(_ => AddGuest());
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
        newTeam.name = GetDefaultTeamName(newTeam, partyModeSettings);
        UpdateTeams();
    }

    private void AddGuest()
    {
        if (partyModeSettings.TeamSettings.Teams.IsNullOrEmpty())
        {
            AddTeam();
        }

        PlayerProfile newGuestProfile = new("", EDifficulty.Medium);
        string newGuestProfileName = GetDefaultGuestProfileName(newGuestProfile, partyModeSettings);
        newGuestProfile.Name = newGuestProfileName;
        partyModeSettings.TeamSettings.Teams.FirstOrDefault().guestPlayerProfiles.Add(newGuestProfile);
        partyModeSettings.GuestPlayerProfiles.Add(newGuestProfile);

        UpdateTeams();
    }

    private void UpdateTeams()
    {
        teamConfigUiRoot.SetVisibleByDisplay(!partyModeSettings.TeamSettings.IsFreeForAll);
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
        teamNameTextField.DisableParseEscapeSequences();
        FieldBindingUtils.Bind(gameObject, teamNameTextField,
            () => team.name,
            newValue => team.name = newValue);
        FieldBindingUtils.ResetValueOnBlurIfEmpty(teamNameTextField);

        // Delete team
        Button deleteTeamButton = teamVisualElement.Q<Button>(R.UxmlNames.deleteTeamButton);
        deleteTeamButton.RegisterCallbackButtonTriggered(_ => DeleteTeam(team));
        deleteTeamButton.SetEnabled(partyModeSettings.TeamSettings.Teams.Count > 1);

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
        otherTeam.playerProfiles.AddRange(team.playerProfiles);
        otherTeam.guestPlayerProfiles.AddRange(team.guestPlayerProfiles);

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
        guestNameTextField.DisableParseEscapeSequences();
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
            playerNameLabel.SetTranslatedText(Translation.Of(playerProfile.Name));
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
        partyModeSettings.TeamSettings.Teams.ForEach(teamSettings =>
        {
            teamSettings.guestPlayerProfiles.Remove(playerProfile);
        });
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
