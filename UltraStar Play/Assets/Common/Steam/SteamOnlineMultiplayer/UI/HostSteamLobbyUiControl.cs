using System;
using CommonOnlineMultiplayer;
using UniInject;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

namespace SteamOnlineMultiplayer
{
    public class HostSteamLobbyUiControl : INeedInjection, IInjectionFinishedListener, IHostLobbyUiControl
    {
        [Inject(UxmlName = R.UxmlNames.hostOnlineGameButton)]
        private Button hostOnlineGameButton;

        [Inject(UxmlName = R.UxmlNames.hostHiddenGameToggle)]
        private Toggle hostHiddenGameToggle;

        [Inject(UxmlName = R.UxmlNames.hostGameNameField)]
        private TextField hostGameNameField;

        [Inject(UxmlName = R.UxmlNames.hostHiddenGamePasswordField)]
        private TextField hostHiddenGamePasswordField;

        [Inject]
        private SteamLobbyMemberManager steamLobbyMemberManager;

        [Inject]
        private SteamLobbyManager steamLobbyManager;

        [Inject]
        private SteamManager steamManager;

        [Inject]
        private NetworkManager networkManager;

        [Inject]
        private Settings settings;

        private string HostGameName => hostGameNameField.value.Trim();
        private bool HostHiddenGame => hostHiddenGameToggle.value;
        private string HostGamePassword => hostHiddenGamePasswordField.value.Trim();

        public void OnInjectionFinished()
        {
            hostGameNameField.value = $"{steamManager.PlayerName} game";
            hostGameNameField.RegisterValueChangedCallback(evt => UpdateControls());

            hostHiddenGameToggle.RegisterValueChangedCallback(evt => UpdateControls());
            hostHiddenGamePasswordField.RegisterValueChangedCallback(evt => UpdateControls());
            hostOnlineGameButton.RegisterCallbackButtonTriggered(evt => HostGameOnSteam());
            UpdateControls();
        }

        private void UpdateControls()
        {
            hostHiddenGamePasswordField.SetEnabled(hostHiddenGameToggle.value);
            hostOnlineGameButton.SetEnabled(!HostGameName.IsNullOrEmpty()
                && (!HostHiddenGame || !HostGamePassword.IsNullOrEmpty()));
        }

        private void HostGameOnSteam()
        {
            bool isHidden = hostHiddenGameToggle.value;
            ESteamLobbyVisibility lobbyVisibility = isHidden
                ? ESteamLobbyVisibility.Private
                : ESteamLobbyVisibility.Public;

            string lobbyPassword = isHidden
                ? HostGamePassword
                : "";

            SteamLobbyConfig steamLobbyConfig = new SteamLobbyConfig()
            {
                name = $"{steamManager.PlayerName}'s game",
                joinable = true,
                maxMembers = SteamConstants.MaxLobbyMembers,
                visibility = lobbyVisibility,
                password = lobbyPassword,
            };

            ObservableUtils.RunOnNewTaskAsObservable(async () =>
                    {
                        if (isHidden
                            && lobbyPassword.IsNullOrEmpty())
                        {
                            throw new OnlineMultiplayerException("Hosting a hidden lobby requires a password");
                        }

                        return await steamLobbyManager.CreateLobbyAsync(steamLobbyConfig);
                    },
                    Disposable.Empty)
                .ObserveOnMainThread()
                .CatchIgnore((Exception ex) =>
                {
                    Debug.LogException(ex);
                    Debug.LogError($"Failed to create lobby: {ex.Message}");
                    NotificationManager.CreateNotification(Translation.Get(R.Messages.onlineGame_error_failedToCreateLobby));
                })
                .Select(lobby=>
                {
                    Debug.Log($"Successfully created lobby: {lobby.Id}. Starting Unity Netcode host with FacepunchTransport.");
                    steamLobbyMemberManager.StartNetcodeNetworkManagerHost();
                    return true;
                })
                .CatchIgnore((Exception ex) =>
                {
                    Debug.LogException(ex);
                    Debug.LogError($"Failed to start Unity Netcode host: {ex.Message}");
                    NotificationManager.CreateNotification(Translation.Get(R.Messages.onlineGame_error_failedToStartNetcodeHost));
                })
                .Subscribe(_ =>
                {
                    NotificationManager.CreateNotification(Translation.Get(R.Messages.onlineGame_hostSuccess));
                });
        }

        public void Dispose()
        {
        }

        public VisualElement CreateVisualElement()
        {
            return Resources.Load<VisualTreeAsset>("HostSteamLobbyUi").CloneTreeAndGetFirstChild();
        }
    }
}
