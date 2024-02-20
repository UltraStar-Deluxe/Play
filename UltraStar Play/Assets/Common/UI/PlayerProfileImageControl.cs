using System;
using CommonOnlineMultiplayer;
using SteamOnlineMultiplayer;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlayerProfileImageControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Optional = true)]
    private MicProfile micProfile;
    public MicProfile MicProfile
    {
        get => micProfile;
        set
        {
            micProfile = value;
            UpdatePlayerProfileImage();
        }
    }

    [Inject(Optional = true)]
    private PlayerProfile playerProfile;
    public PlayerProfile PlayerProfile
    {
        get
        {
            return playerProfile;
        }
        set
        {
            playerProfile = value;
            UpdatePlayerProfileImage();
        }
    }

    [Inject]
    private UiManager uiManager;

    [Inject]
    private Settings settings;

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    private VisualElement image;

    [Inject]
    private OnlineMultiplayerManager onlineMultiplayerManager;

    private LobbyMember lobbyMember;

    public void OnInjectionFinished()
    {
        if (playerProfile is LobbyMemberPlayerProfile lobbyMemberPlayerProfile)
        {
            lobbyMember = onlineMultiplayerManager.LobbyMemberManager.GetLobbyMember(lobbyMemberPlayerProfile.UnityNetcodeClientId);
        }

        UpdatePlayerProfileImage();
    }

    private void UpdatePlayerProfileImage()
    {
        if (playerProfile == null)
        {
            return;
        }

        UpdatePlayerImageColors();

        if (lobbyMember == null)
        {
            string finalImagePath = uiManager.GetFinalPlayerProfileImagePath(playerProfile);
            uiManager.LoadPlayerProfileImage(finalImagePath)
                .Subscribe(loadedSprite => image.style.backgroundImage = new StyleBackground(loadedSprite));
        }
        else if (lobbyMember is SteamLobbyMember steamLobbyMember)
        {
            SteamOnlineMultiplayerUtils.GetAvatarTextureAsObservable(steamLobbyMember.SteamId)
                .CatchIgnore((Exception ex) =>
                {
                    Debug.LogException(ex);
                    Debug.LogError(
                        $"Failed to get avatar image of player with Steam id {steamLobbyMember.SteamId}: {ex.Message}");
                })
                .Subscribe(texture =>
                {
                    image.style.backgroundImage = new StyleBackground(texture);
                });
        }
    }

    private void UpdatePlayerImageColors()
    {
        UpdatePlayerImageBackgroundColor();
        UpdatePlayerImageTintColor();
    }

    private void UpdatePlayerImageTintColor()
    {
        if (lobbyMember != null
            && lobbyMember is not SteamLobbyMember)
        {
            image.style.unityBackgroundImageTintColor = new StyleColor(CommonOnlineMultiplayerUtils.GetPlayerColor(playerProfile, micProfile));
        }
    }

    private void UpdatePlayerImageBackgroundColor()
    {
        if (micProfile != null)
        {
            image.style.backgroundColor = new StyleColor(micProfile.Color);
            return;
        }

        image.style.backgroundColor =  new StyleColor(Color.clear);
    }
}
