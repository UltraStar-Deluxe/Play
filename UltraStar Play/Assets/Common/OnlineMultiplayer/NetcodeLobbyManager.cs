using UniInject;
using Unity.Netcode;

namespace CommonOnlineMultiplayer
{
    public class NetcodeLobbyManager : AbstractSingletonBehaviour, INeedInjection, ILobbyManager
    {
        public static NetcodeLobbyManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<NetcodeLobbyManager>();

        [Inject]
        private NetworkManager networkManager;

        protected override object GetInstance()
        {
            return Instance;
        }

        public ILobby CurrentLobby
        {
            get
            {
                if (networkManager.IsClient)
                {
                    return new NetcodeLobby("Direct Connection");
                }

                return null;
            }
        }

        public void LeaveCurrentLobby()
        {
            networkManager.Shutdown();
        }

        public class NetcodeLobby : ILobby
        {
            private readonly string name;
            public string Name => name;

            public NetcodeLobby(string name)
            {
                this.name = name;
            }
        }
    }
}
