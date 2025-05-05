using CommonOnlineMultiplayer;
using UniInject;

namespace SteamOnlineMultiplayer
{
    public class SteamOnlineMultiplayerBackendConfigurator : AbstractSingletonBehaviour, INeedInjection
    {
        public static SteamOnlineMultiplayerBackendConfigurator Instance => DontDestroyOnLoadManager.FindComponentOrThrow<SteamOnlineMultiplayerBackendConfigurator>();

        [Inject]
        private OnlineMultiplayerManager onlineMultiplayerManager;

        [Inject]
        private SteamLobbyManager steamLobbyManager;

        [Inject]
        private SteamLobbyMemberManager steamLobbyMemberManager;

        protected override object GetInstance()
        {
            return Instance;
        }

        protected override void StartSingleton()
        {
            onlineMultiplayerManager.BackendManager.AddBackend(new OnlineMultiplayerBackend(
                EOnlineMultiplayerBackend.Steam,
                steamLobbyManager,
                steamLobbyMemberManager,
                () => new HostSteamLobbyUiControl(),
                () => new JoinSteamLobbyUiControl(),
                () => new CurrentSteamLobbyUiControl()));
        }
    }
}
