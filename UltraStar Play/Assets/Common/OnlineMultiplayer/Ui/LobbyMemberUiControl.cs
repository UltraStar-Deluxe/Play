using UniInject;
using Unity.Netcode;
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

        [Inject(UxmlName = R.UxmlNames.playerProfileImagePicker)]
        protected ItemPicker playerProfileImagePicker;

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

            playerProfileImagePicker.ItemLabel.HideByDisplay();
            playerProfileImagePicker.PreviousItemButton.HideByDisplay();
            playerProfileImagePicker.NextItemButton.HideByDisplay();
            UpdateImage();
        }

        protected virtual void UpdateImage()
        {
            Image imageElement = playerProfileImagePicker.ItemImage;
            imageElement.style.backgroundImage = new StyleBackground(UiManager.Instance.fallbackPlayerProfileImage);
            imageElement.style.unityBackgroundImageTintColor = new StyleColor(ColorGenerationUtils.FromString(lobbyMember.DisplayName));
        }

        protected virtual void RemoveLobbyMember()
        {
            if (networkManager.IsServer)
            {
                if (lobbyMember.UnityNetcodeClientId == networkManager.LocalClientId)
                {
                    // Disconnect own client by shutting down the server
                    networkManager.Shutdown();
                }
                else
                {
                    // Disconnect other client
                    networkManager.DisconnectClient(lobbyMember.UnityNetcodeClientId, "Kicked by host");
                }
            }
        }
    }
}
