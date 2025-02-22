using UniInject;

namespace CommonOnlineMultiplayer
{
    public class NetcodeOnlineMultiplayerBackendConfigurator : AbstractSingletonBehaviour, INeedInjection
    {
        public static NetcodeOnlineMultiplayerBackendConfigurator Instance => DontDestroyOnLoadManager.FindComponentOrThrow<NetcodeOnlineMultiplayerBackendConfigurator>();

        [Inject]
        private OnlineMultiplayerManager onlineMultiplayerManager;

        [Inject]
        private NetcodeLobbyManager netcodeLobbyManager;

        [Inject]
        private NetcodeLobbyMemberManager netcodeLobbyMemberManager;

        protected override object GetInstance()
        {
            return Instance;
        }

        protected override void StartSingleton()
        {
            onlineMultiplayerManager.BackendManager.AddBackend(new OnlineMultiplayerBackend(
                EOnlineMultiplayerBackend.Netcode,
                netcodeLobbyManager,
                netcodeLobbyMemberManager,
                () => new HostNetcodeLobbyUiControl(),
                () => new JoinNetcodeLobbyUiControl(),
                () => new CurrentNetcodeLobbyUiControl()));
        }
    }
}
