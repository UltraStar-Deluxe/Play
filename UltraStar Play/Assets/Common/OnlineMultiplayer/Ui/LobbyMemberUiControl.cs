using UniInject;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace CommonOnlineMultiplayer
{
    public class LobbyMemberUiControl : INeedInjection, IInjectionFinishedListener
    {
        [Inject]
        protected NetworkManager networkManager;

        [Inject]
        protected LobbyMember lobbyMember;

        [Inject]
        protected ILobbyMemberManager lobbyMemberManager;

        [Inject(UxmlName = R.UxmlNames.nameTextField)]
        protected TextField nameTextField;

        [Inject(UxmlName = R.UxmlNames.enabledToggle)]
        protected VisualElement enabledToggle;

        [Inject(UxmlName = R.UxmlNames.deleteButton)]
        protected Button deleteButton;

        [Inject(UxmlName = R.UxmlNames.webCamButtonOverlay)]
        protected VisualElement webCamButtonOverlay;

        [Inject(UxmlName = R.UxmlNames.playerProfileImageChooser)]
        protected Chooser playerProfileImageChooser;

        [Inject(UxmlName = R.UxmlNames.onlinePlayerProfileIcon)]
        protected VisualElement onlinePlayerProfileIcon;

        public virtual void OnInjectionFinished()
        {
            string displayName = lobbyMember.DisplayName;

            enabledToggle.HideByDisplay();

            deleteButton.RegisterCallbackButtonTriggered(_ => RemoveLobbyMember());
            deleteButton.SetVisibleByDisplay(networkManager.IsServer);
            deleteButton.SetEnabled(lobbyMember.UnityNetcodeClientId != networkManager.LocalClientId);

            nameTextField.isReadOnly = true;
            nameTextField.value = displayName;

            webCamButtonOverlay.HideByDisplay();

            playerProfileImageChooser.ItemLabel.HideByDisplay();
            playerProfileImageChooser.PreviousItemButton.HideByDisplay();
            playerProfileImageChooser.NextItemButton.HideByDisplay();
            playerProfileImageChooser.ItemImage.AddToClassList("circle");
            onlinePlayerProfileIcon.SetInClassList("onlineMultiplayerHost", lobbyMember.IsHost);
            UpdateImage();
        }

        protected virtual void UpdateImage()
        {
            Image imageElement = playerProfileImageChooser.ItemImage;
            imageElement.style.backgroundImage = new StyleBackground(PlayerProfileImageManager.Instance.fallbackPlayerProfileImage);
            imageElement.style.unityBackgroundImageTintColor = new StyleColor(ColorGenerationUtils.FromString(lobbyMember.DisplayName));
        }

        protected virtual void RemoveLobbyMember()
        {
            if (networkManager.IsServer)
            {
                if (lobbyMember.UnityNetcodeClientId == NetworkManager.ServerClientId)
                {
                    // Disconnect own client by shutting down the server
                    networkManager.ShutdownIfConnectedClient("Disconnecting own player from online game, which is the host");
                }
                else
                {
                    // Disconnect other client
                    Debug.Log($"Disconnecting other player with Netcode id {lobbyMember.UnityNetcodeClientId} from online game");
                    networkManager.DisconnectClient(lobbyMember.UnityNetcodeClientId, "Kicked by host");
                }
            }
        }
    }
}
