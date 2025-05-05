using System;
using System.Collections.Generic;
using System.Linq;
using CommonOnlineMultiplayer;
using SteamOnlineMultiplayer;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlayerProfileOptionsSceneControl : AbstractOptionsSceneControl, INeedInjection
{
    [InjectedInInspector]
    public VisualTreeAsset playerProfileListEntryAsset;

    [Inject(UxmlName = R.UxmlNames.playerProfileList)]
    private ScrollView playerProfileList;

    [Inject(UxmlName = R.UxmlNames.addButton)]
    private Button addButton;

    [Inject]
    private PlayerProfileImageManager playerProfileImageManager;

    [Inject]
    private WebCamManager webCamManager;

    [Inject]
    private OnlineMultiplayerManager onlineMultiplayerManager;

    protected override void Start()
    {
        base.Start();

        UpdatePlayerProfileList();

        addButton.RegisterCallbackButtonTriggered(_ =>
        {
            PlayerProfile newPlayerProfile = new PlayerProfile();
            newPlayerProfile.Name = GetNewPlayerProfileName();
            settings.PlayerProfiles.Add(newPlayerProfile);
            VisualElement playerProfileEntryVisualElement = CreatePlayerProfileEntry(newPlayerProfile);
            playerProfileEntryVisualElement.RegisterHasGeometryCallbackOneShot(_ => playerProfileEntryVisualElement.ScrollToSelf());

            // Focus on the name of the newly added player to directly allow changing its name
            TextField nameTextField = playerProfileList[playerProfileList.childCount - 1].Q<TextField>("nameTextField");
            nameTextField.DisableParseEscapeSequences();
            nameTextField.Focus();
        });
    }

    private string GetNewPlayerProfileName()
    {
        bool ExistsPlayerProfileWithName(string newName)
        {
            return settings.PlayerProfiles.Any(playerProfile =>
            {
                string nameWithoutWhiteSpace = playerProfile.Name.Replace(" ", "");
                string newNameWithoutWhiteSpace = newName.Replace(" ", "");
                return nameWithoutWhiteSpace.Equals(newNameWithoutWhiteSpace, StringComparison.InvariantCultureIgnoreCase);
            });
        }

        int index = 1;
        string playerProfileName = $"Player{index:00}";
        while (ExistsPlayerProfileWithName(playerProfileName))
        {
            index++;
            playerProfileName = $"Player{index:00}";
        }

        return playerProfileName;
    }

    private void UpdatePlayerProfileList()
    {
        playerProfileList.Clear();
        settings.PlayerProfiles
            .Union(nonPersistentSettings.LobbyMemberPlayerProfiles)
            .ForEach(playerProfile => CreatePlayerProfileEntry(playerProfile));
    }

    private void UpdatePlayerProfileInactiveOverlay(PlayerProfile playerProfile, VisualElement playerProfileInactiveOverlay)
    {
        playerProfileInactiveOverlay.ShowByDisplay();
        playerProfileInactiveOverlay.SetInClassList("hidden", playerProfile.IsEnabled);
    }

    private int GetIndexInList(PlayerProfile playerProfile)
    {
        // Dynamically return index in list because the list can change while the scene is open.
        return settings.PlayerProfiles.IndexOf(playerProfile);
    }

    private VisualElement CreatePlayerProfileEntry(PlayerProfile playerProfile)
    {
        VisualElement visualElement = playerProfileListEntryAsset.CloneTree().Children().FirstOrDefault();

        VisualElement playerProfileInactiveOverlay = visualElement.Q<VisualElement>(R.UxmlNames.playerProfileInactiveOverlay);

        Button deleteButton = visualElement.Q<Button>(R.UxmlNames.deleteButton);
        deleteButton.RegisterCallbackButtonTriggered(_ =>
        {
            if (!settings.PlayerProfiles.Contains(playerProfile))
            {
                return;
            }

            if (GetIndexInList(playerProfile) < settings.PlayerProfiles.Count)
            {
                settings.PlayerProfiles.RemoveAt(GetIndexInList(playerProfile));
            }
            visualElement.RemoveFromHierarchy();
        });

        TextField nameTextField = visualElement.Q<TextField>(R.UxmlNames.nameTextField);
        nameTextField.isReadOnly = playerProfile is LobbyMemberPlayerProfile;
        nameTextField.DisableParseEscapeSequences();
        nameTextField.value = playerProfile.Name;
        nameTextField.RegisterValueChangedCallback(evt => playerProfile.Name = evt.newValue);

        SlideToggle enabledToggle = visualElement.Q<SlideToggle>(R.UxmlNames.enabledToggle);
        enabledToggle.value = playerProfile.IsEnabled;
        enabledToggle.RegisterValueChangedCallback(evt =>
        {
            playerProfile.IsEnabled = evt.newValue;
            UpdatePlayerProfileInactiveOverlay(playerProfile, playerProfileInactiveOverlay);
        });
        UpdatePlayerProfileInactiveOverlay(playerProfile, playerProfileInactiveOverlay);

        PlayerProfileImageChooserControl playerProfileImageChooserControl = new PlayerProfileImageChooserControl(visualElement.Q<Chooser>(R.UxmlNames.playerProfileImageChooser), GetIndexInList(playerProfile), playerProfileImageManager, webCamManager);
        playerProfileImageChooserControl.Bind(() => playerProfile.ImagePath,
                newValue => playerProfile.ImagePath = newValue);

        EnumChooserControl<EDifficulty> difficultyChooser = new(visualElement.Q<Chooser>(R.UxmlNames.difficultyChooser));
        difficultyChooser.Bind(() => playerProfile.Difficulty,
                newValue => playerProfile.Difficulty = newValue);

        playerProfileList.Add(visualElement);

        VisualElement onlinePlayerProfileIconContainer = visualElement.Q<VisualElement>(R.UxmlNames.onlinePlayerProfileIconContainer);
        VisualElement onlinePlayerProfileIcon = visualElement.Q<VisualElement>(R.UxmlNames.onlinePlayerProfileIcon);

        if (playerProfile is LobbyMemberPlayerProfile lobbyMemberPlayerProfile)
        {
            enabledToggle.HideByDisplay();
            deleteButton.HideByDisplay();
            difficultyChooser.Chooser.HideByDisplay();
            playerProfileImageChooserControl.Chooser.PreviousItemButton.HideByDisplay();
            playerProfileImageChooserControl.Chooser.NextItemButton.HideByDisplay();
            onlinePlayerProfileIcon.SetInClassList("onlineMultiplayerHost", lobbyMemberPlayerProfile.IsHost);
            onlinePlayerProfileIconContainer.ShowByDisplay();

            UpdateOnlineMultiplayerPlayerImage(lobbyMemberPlayerProfile, playerProfileImageChooserControl);
        }
        else
        {
            onlinePlayerProfileIconContainer.HideByDisplay();
        }

        return visualElement;
    }

    private async void UpdateOnlineMultiplayerPlayerImage(LobbyMemberPlayerProfile lobbyMemberPlayerProfile, PlayerProfileImageChooserControl playerProfileImageChooserControl)
    {
        if (settings.EOnlineMultiplayerBackend is EOnlineMultiplayerBackend.Netcode)
        {
            playerProfileImageChooserControl.Chooser.ItemLabel.style.unityBackgroundImageTintColor = new StyleColor(ColorGenerationUtils.FromString(lobbyMemberPlayerProfile.Name));
        }
        else if (settings.EOnlineMultiplayerBackend is EOnlineMultiplayerBackend.Steam)
        {
            SteamLobbyMember steamLobbyMember = onlineMultiplayerManager.LobbyMemberManager.GetLobbyMember(lobbyMemberPlayerProfile.UnityNetcodeClientId) as SteamLobbyMember;
            if (steamLobbyMember == null)
            {
                return;
            }

            try
            {
                Texture2D texture = await SteamAvatarImageUtils.GetAvatarTextureAsync(steamLobbyMember.SteamId);
                playerProfileImageChooserControl.Chooser.ItemImage.image = texture;
                playerProfileImageChooserControl.Chooser.ItemLabel.HideByDisplay();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to get avatar image of Steam user '{steamLobbyMember.DisplayName}' with id {steamLobbyMember.SteamId}");
            }
        }
    }

    public override string SteamWorkshopUri => "https://steamcommunity.com/workshop/browse/?appid=2394070&requiredtags[]=PlayerProfileImage";

    public override string HelpUri => Translation.Get(R.Messages.uri_howToPlayerProfiles);
}
