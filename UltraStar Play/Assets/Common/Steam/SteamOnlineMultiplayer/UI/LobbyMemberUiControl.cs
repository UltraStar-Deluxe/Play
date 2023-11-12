using System;
using Steamworks;
using UniInject;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace SteamOnlineMultiplayer
{
    public class LobbyMemberUiControl : INeedInjection, IInjectionFinishedListener
    {
        [Inject]
        private NetworkManager networkManager;

        [Inject]
        private SteamLobbyMember steamLobbyMember;

        [Inject]
        private SteamMultiplayerManager steamMultiplayerManager;

        [Inject]
        private SteamManager steamManager;

        [Inject(UxmlName = R.UxmlNames.nameTextField)]
        private TextField nameTextField;

        [Inject(UxmlName = R.UxmlNames.enabledToggle)]
        private VisualElement enabledToggle;

        [Inject(UxmlName = R.UxmlNames.deleteButton)]
        private Button deleteButton;

        [Inject(UxmlName = R.UxmlNames.webCamButtonOverlay)]
        private VisualElement webCamButtonOverlay;

        [Inject(UxmlName = R.UxmlNames.playerProfileImagePicker)]
        private ItemPicker playerProfileImagePicker;

        public void OnInjectionFinished()
        {
            string displayName = steamLobbyMember.DisplayName;
            SteamId steamId = steamLobbyMember.SteamId;

            enabledToggle.HideByDisplay();

            deleteButton.RegisterCallbackButtonTriggered(_ => RemoveClient());
            deleteButton.SetVisibleByDisplay(networkManager.IsServer);
            deleteButton.SetEnabled(steamLobbyMember.UnityNetcodeClientId != networkManager.LocalClientId);

            nameTextField.isReadOnly = true;
            nameTextField.value = displayName;

            webCamButtonOverlay.HideByDisplay();

            playerProfileImagePicker.ItemLabel.HideByDisplay();
            playerProfileImagePicker.PreviousItemButton.HideByDisplay();
            playerProfileImagePicker.NextItemButton.HideByDisplay();
            UpdateImage(playerProfileImagePicker.ItemImage, steamId, displayName);
        }

        private void UpdateImage(Image itemImage, SteamId steamId, string displayName)
        {
            itemImage.SetBorderRadius(Length.Percent(50));
            ObservableUtils.RunOnNewTaskAsObservable(async () =>
                {
                    return await SteamOnlineMultiplayerUtils.GetAvatarTextureAsync(steamId);
                })
                .ObserveOnMainThread()
                .CatchIgnore((Exception ex) =>
                {
                    Debug.LogException(ex);
                    Debug.LogError($"Failed to get avatar image of Steam user '{displayName}' with id {steamId}");
                    itemImage.style.backgroundImage = new StyleBackground(UiManager.Instance.fallbackPlayerProfileImage);
                    itemImage.style.unityBackgroundImageTintColor = new StyleColor(ColorGenerationUtils.FromString(displayName));
                })
                .Subscribe(texture =>
                {
                    itemImage.image = texture;
                });
        }

        private void RemoveClient()
        {
            if (networkManager.IsServer)
            {
                if (steamLobbyMember.UnityNetcodeClientId == networkManager.LocalClientId)
                {
                    // Disconnect own client by shutting down the server
                    networkManager.Shutdown();
                }
                else
                {
                    // Disconnect other client
                    networkManager.DisconnectClient(steamLobbyMember.UnityNetcodeClientId, "Kicked by host");
                }
            }
        }
    }
}
