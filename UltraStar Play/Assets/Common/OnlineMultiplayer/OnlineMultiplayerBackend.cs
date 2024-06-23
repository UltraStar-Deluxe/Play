using System;

namespace CommonOnlineMultiplayer
{
    public class OnlineMultiplayerBackend
    {
        public EOnlineMultiplayerBackend Backend { get; private set; }
        public ILobbyManager LobbyManager { get; private set; }
        public ILobbyMemberManager LobbyMemberManager { get; private set; }

        public Func<IHostLobbyUiControl> HostLobbyUiControlFactory { get; private set; }
        public Func<IJoinLobbyUiControl> JoinLobbyUiControlFactory { get; private set; }
        public Func<ICurrentLobbyUiControl> CurrentLobbyUiControlFactory { get; private set; }

        public OnlineMultiplayerBackend(
            EOnlineMultiplayerBackend backend,
            ILobbyManager lobbyManager,
            ILobbyMemberManager lobbyMemberManager,
            Func<IHostLobbyUiControl> hostLobbyUiControlFactory,
            Func<IJoinLobbyUiControl> joinLobbyUiControlFactory,
            Func<ICurrentLobbyUiControl> currentLobbyUiControlFactory)
        {
            Backend = backend;
            LobbyManager = lobbyManager;
            LobbyMemberManager = lobbyMemberManager;
            HostLobbyUiControlFactory = hostLobbyUiControlFactory;
            JoinLobbyUiControlFactory = joinLobbyUiControlFactory;
            CurrentLobbyUiControlFactory = currentLobbyUiControlFactory;
        }
    }
}
