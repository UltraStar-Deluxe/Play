using System;
using CommonOnlineMultiplayer;
using Steamworks.Data;
using UniInject;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

namespace SteamOnlineMultiplayer
{
    public class JoinSteamLobbyUiControl : INeedInjection, IInjectionFinishedListener, IJoinLobbyUiControl
    {
        [Inject(UxmlName = R.UxmlNames.joinHiddenGamePasswordField)]
        private TextField joinHiddenGamePasswordField;

        [Inject(UxmlName = R.UxmlNames.hostedGameList)]
        private VisualElement hostedGameList;

        [Inject(UxmlName = R.UxmlNames.hostedGameListTitle)]
        private VisualElement hostedGameListTitle;

        [Inject(UxmlName = R.UxmlNames.searchHostedGamesButton)]
        private Button searchHostedGamesButton;

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

        private string JoinGamePassword => joinHiddenGamePasswordField.value.Trim();

        public void OnInjectionFinished()
        {
            searchHostedGamesButton.RegisterCallbackButtonTriggered(_ => UpdateHostedGameList());
            UpdateHostedGameList();
        }

        private async void JoinGameOnSteam(Lobby lobby)
        {
            Lobby joinedLobby;
            try
            {
                await Awaitable.BackgroundThreadAsync();
                joinedLobby = await steamLobbyManager.JoinLobbyAsync(lobby);
                await Awaitable.MainThreadAsync();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to join lobby: {ex.Message}");
                NotificationManager.CreateNotification(Translation.Get(R.Messages.onlineGame_error_failedToJoinLobby));
                return;
            }

            ulong joinedLobbyOwnerId = joinedLobby.Owner.Id;
            if (joinedLobbyOwnerId <= 0)
            {
                throw new OnlineMultiplayerException($"Successfully joined lobby '{joinedLobby.GetName()}' with id {joinedLobby.Id} but owner id is 0.");
            }

            Debug.Log($"Successfully joined lobby '{joinedLobby.GetName()}' with id {joinedLobby.Id} and owner {joinedLobby.Owner}. Starting Unity Netcode client with FacepunchTransport.");
            steamLobbyMemberManager.StartNetcodeNetworkManagerClient(joinedLobbyOwnerId);
            NotificationManager.CreateNotification(Translation.Get(R.Messages.onlineGame_joinSuccess));
        }

        private async void UpdateHostedGameList()
        {
            string lobbyPassword = JoinGamePassword;

            hostedGameList.Clear();

            Lobby[] lobbies;
            try
            {
                await Awaitable.BackgroundThreadAsync();
                lobbies = await steamLobbyManager.GetLobbiesAsync(lobbyPassword);
                await Awaitable.MainThreadAsync();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                NotificationManager.CreateNotification(Translation.Get(R.Messages.common_errorWithReason,
                    "reason", ex.Message));
                hostedGameList.Add(new Label("Failed to fetch lobbies"));
                return;
            }

            FillHostedGameList(lobbies);
        }

        private void FillHostedGameList(Lobby[] lobbies)
        {
            bool hasHostedGames = !lobbies.IsNullOrEmpty();
            hostedGameListTitle.SetVisibleByDisplay(hasHostedGames);
            if (hasHostedGames)
            {
                hostedGameListTitle.ShowByDisplay();
                foreach (Lobby lobby in lobbies)
                {
                    Button joinLobbyButton = new Button();
                    joinLobbyButton.SetTranslatedText(Translation.Get(R.Messages.onlineGame_lobby_join,
                        "lobbyName", lobby.GetName()));
                    joinLobbyButton.RegisterCallbackButtonTriggered(_ => JoinGameOnSteam(lobby));
                    hostedGameList.Add(joinLobbyButton);
                }
            }
            else
            {
                hostedGameList.Add(new Label());
                if (JoinGamePassword.IsNullOrEmpty())
                {
                    hostedGameList.Add(new Label(Translation.Get(R.Messages.onlineGame_lobby_notFound_tryPasswordHint)));
                }
                else
                {
                    hostedGameList.Add(new Label(Translation.Get(R.Messages.onlineGame_lobby_notFound_tryOtherPasswordHint)));
                }
            }

            hostedGameList.GetFirstAncestorOfType<AccordionItem>()?.UpdateTargetHeight();
        }

        public void Dispose()
        {
        }

        public VisualElement CreateVisualElement()
        {
            return Resources.Load<VisualTreeAsset>("JoinSteamLobbyUi").CloneTreeAndGetFirstChild();
        }
    }
}
