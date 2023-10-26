using System;
using SteamOnlineMultiplayer;
using Steamworks.Data;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

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

    public void OnInjectionFinished()
    {
        hostGameButton.RegisterCallbackButtonTriggered(_ => CreateLobby());
        joinGameButton.RegisterCallbackButtonTriggered(_ => JoinLobby(joinLobbyPasswordField.value));

        UpdateLobbyList();
    }

    private void JoinLobby(string lobbyCode)
    {
        SteamMultiplayerClientManager.Instance.JoinLobby(lobbyCode);
    }

    private void CreateLobby()
    {
        string lobbyName = $"Lobby {Guid.NewGuid()}";
        bool joinable = true;
        byte maxMembers = 32;
        ESteamLobbyVisibility visibility = ESteamLobbyVisibility.Public;

        ObservableUtils.RunOnNewTaskAsObservable(async () =>
            {
                bool success = await SteamMultiplayerNetworkManager.Instance.StartSteamHost(new SteamOnlineMultiplayerExtensions.LobbyConfig()
                {
                    name = lobbyName,
                    joinable = joinable,
                    maxMembers = maxMembers,
                    visibility = visibility,
                });

                if (!success)
                {
                    throw new Exception("Failed to host game");
                }
                return true;
            },
            Disposable.Empty)
            .ObserveOnMainThread()
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to create lobby: {ex.Message}");
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
            Lobby[] lobbies = await SteamOnlineMultiplayerExtensions.GetLobbiesAsync() ?? Array.Empty<Lobby>();
            return lobbies;
        },
        Disposable.Empty)
            .ObserveOnMainThread()
            .CatchIgnore((Exception ex) =>
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to fetch lobbies: {ex.Message}");
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
