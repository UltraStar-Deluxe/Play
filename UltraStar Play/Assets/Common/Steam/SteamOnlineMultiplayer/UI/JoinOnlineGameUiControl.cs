using System;
using System.Text;
using CommonOnlineMultiplayer;
using Steamworks.Data;
using UniInject;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

namespace SteamOnlineMultiplayer
{
    public class JoinOnlineGameUiControl : INeedInjection, IInjectionFinishedListener, IDisposable
    {
        [Inject(UxmlName = R.UxmlNames.joinHiddenGamePasswordField)]
        private TextField joinHiddenGamePasswordField;

        [Inject(UxmlName = R.UxmlNames.hostedGameList)]
        private VisualElement hostedGameList;

        [Inject(UxmlName = R.UxmlNames.searchHostedGamesButton)]
        private Button searchHostedGamesButton;

        [Inject(UxmlName = R.UxmlNames.joinOnlineGameDirectlyButton)]
        private Button joinOnlineGameDirectlyButton;

        [Inject]
        private SteamMultiplayerManager steamMultiplayerManager;

        [Inject]
        private SteamLobbyManager steamLobbyManager;

        [Inject]
        private SteamManager steamManager;

        [Inject]
        private NetworkManager networkManager;

        [Inject]
        private Settings settings;

        private string JoinGamePassword => joinHiddenGamePasswordField.value.Trim();

        public void OnInjectionFinished()
        {
            joinOnlineGameDirectlyButton.RegisterCallbackButtonTriggered(_ => JoinGameDirectly());
            searchHostedGamesButton.RegisterCallbackButtonTriggered(_ => UpdateHostedGameList());
            UpdateHostedGameList();
        }

        private void JoinGameOnSteam(Lobby lobby)
        {
            ObservableUtils.RunOnNewTaskAsObservable(async () =>
                    {
                        return await steamLobbyManager.JoinLobbyAsync(lobby);
                    },
                    Disposable.Empty)
                .ObserveOnMainThread()
                .CatchIgnore((Exception ex) =>
                {
                    Debug.LogException(ex);
                    Debug.LogError($"Failed to join lobby: {ex.Message}");
                    UiManager.CreateNotification("Failed to join lobby");
                })
                .Select(joinedLobby =>
                {
                    ulong joinedLobbyOwnerId = joinedLobby.Owner.Id;
                    if (joinedLobbyOwnerId <= 0)
                    {
                        throw new OnlineMultiplayerException($"Successfully joined lobby '{joinedLobby.GetName()}' with id {joinedLobby.Id} but owner id is 0.");
                    }

                    Debug.Log($"Successfully joined lobby '{joinedLobby.GetName()}' with id {joinedLobby.Id} and owner {joinedLobby.Owner}. Starting Unity Netcode client with FacepunchTransport.");
                    steamMultiplayerManager.StartNetcodeNetworkManagerClient(joinedLobbyOwnerId);
                    return true;
                })
                .Subscribe(_ =>
                {
                    UiManager.CreateNotification("Successfully joined online game");
                });
        }

        private void UpdateHostedGameList()
        {
            string lobbyPassword = JoinGamePassword;

            hostedGameList.Clear();

            ObservableUtils.RunOnNewTaskAsObservable(async () =>
                {
                    Lobby[] lobbies = await steamLobbyManager.GetLobbiesAsync(lobbyPassword);
                    return lobbies;
                },
                Disposable.Empty)
                .ObserveOnMainThread()
                .CatchIgnore((Exception ex) =>
                {
                    Debug.LogException(ex);
                    UiManager.CreateNotification($"Failed to update lobby list: {ex.Message}");
                    hostedGameList.Add(new Label("Failed to fetch lobbies"));
                })
                .Subscribe(lobbies => FillHostedGameList(lobbies));
        }

        private void FillHostedGameList(Lobby[] lobbies)
        {
            if (lobbies.IsNullOrEmpty())
            {
                hostedGameList.Add(new Label("No online games found."));
                if (JoinGamePassword.IsNullOrEmpty())
                {
                    hostedGameList.Add(new Label("Try to enter a password to search hidden games."));
                }
                else
                {
                    hostedGameList.Add(new Label("Try a different password to search hidden games."));
                }
            }
            else
            {
                foreach (Lobby lobby in lobbies)
                {
                    Button joinLobbyButton = new Button();
                    joinLobbyButton.text = $"Join \"{lobby.GetName()}\", members: {lobby.MemberCount}";
                    joinLobbyButton.RegisterCallbackButtonTriggered(_ => JoinGameOnSteam(lobby));
                    hostedGameList.Add(joinLobbyButton);
                }
            }

            hostedGameList.GetFirstAncestorOfType<AccordionItem>()?.UpdateTargetHeight();

            ThemeManager.ApplyThemeSpecificStylesToVisualElements(hostedGameList);
        }

        private void JoinGameDirectly()
        {
            CommonOnlineMultiplayerUtils.ConfigureUnityTransport(networkManager, settings);

            NetworkPlayerConnectionRequestDataDto requestDataDto = new(
                123456789,
                Guid.NewGuid().ToString(),
                "Dummy Client",
                SceneManager.GetActiveScene().name);
            string payload = requestDataDto.ToJson();
            networkManager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(payload);
            networkManager.StartClient();
        }

        public void Dispose()
        {
        }
    }
}
