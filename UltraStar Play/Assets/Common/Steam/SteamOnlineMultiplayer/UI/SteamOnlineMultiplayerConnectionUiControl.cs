using System;
using Steamworks;
using Steamworks.Data;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

namespace SteamOnlineMultiplayer
{
    public class OnlineMultiplayerConnectionUiControl : MonoBehaviour, INeedInjection, IInjectionFinishedListener
    {
        [Inject(UxmlName = R.UxmlNames.hostGameButton)]
        private Button hostGameButton;

        [Inject(UxmlName = R.UxmlNames.hostGameVisibilityChooser)]
        private DropdownField hostGameVisibilityChooser;

        [Inject(UxmlName = R.UxmlNames.hostLobbyPasswordField)]
        private TextField hostLobbyPasswordField;

        [Inject(UxmlName = R.UxmlNames.joinGameButton)]
        private Button joinGameButton;

        [Inject(UxmlName = R.UxmlNames.joinLobbyPasswordField)]
        private TextField joinLobbyPasswordField;

        [Inject(UxmlName = R.UxmlNames.lobbyList)]
        private VisualElement lobbyList;

        [Inject]
        private SteamMultiplayerManager steamMultiplayerManager;

        public void OnInjectionFinished()
        {
            hostGameButton.RegisterCallbackButtonTriggered(_ => CreateLobby());
            joinGameButton.RegisterCallbackButtonTriggered(_ => JoinLobby(joinLobbyPasswordField.value));

            UpdateLobbyList();
        }

        private void JoinLobby(string lobbyCode)
        {
            steamMultiplayerManager.JoinLobbyByCode(lobbyCode);
        }

        private void CreateLobby()
        {
            ObservableUtils.RunOnNewTaskAsObservable(async () =>
                    {
                        await steamMultiplayerManager.CreateLobbyAndStartHostAsync(
                            new LobbyConfig()
                            {
                                name = $"Lobby {Guid.NewGuid()}",
                                joinable = true,
                                maxMembers = SteamConstants.MaxLobbyMembers,
                                visibility = ELobbyVisibility.Public,
                            });

                        return true;
                    },
                    Disposable.Empty)
                .ObserveOnMainThread()
                .CatchIgnore((Exception ex) =>
                {
                    Debug.LogException(ex);
                    UiManager.CreateNotification(ex.Message);
                })
                .Subscribe(_ =>
                {
                    Debug.Log("Successfully created new lobby, now acting as host.");
                });
        }

        private void UpdateLobbyList()
        {
            lobbyList.Clear();

            ObservableUtils.RunOnNewTaskAsObservable(async () =>
                    {
                        Lobby[] lobbies = await SteamOnlineMultiplayerUtils.GetLobbiesAsync();
                        return lobbies;
                    },
                    Disposable.Empty)
                .ObserveOnMainThread()
                .CatchIgnore((Exception ex) =>
                {
                    Debug.LogException(ex);
                    UiManager.CreateNotification($"Failed to update lobby list: {ex.Message}");
                    lobbyList.Add(new Label("Failed to fetch lobbies"));
                })
                .Subscribe(lobbies =>
                {
                    foreach (Lobby lobby in lobbies)
                    {
                        VisualElement lobbyElement = new Label($"Lobby: {lobby.Id}");
                        lobbyList.Add(lobbyElement);
                    }
                });
        }
    }
}
