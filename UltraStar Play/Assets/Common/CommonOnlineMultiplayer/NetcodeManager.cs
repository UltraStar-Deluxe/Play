using UniInject;
using Unity.Netcode;

namespace CommonOnlineMultiplayer
{
    public class NetcodeManager : AbstractSingletonBehaviour, INeedInjection
    {
        public static NetcodeManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<NetcodeManager>();

        [Inject]
        private NetworkManager networkManager;

        [Inject]
        private NetcodeMessagingManager netcodeMessagingManager;

        protected override object GetInstance()
        {
            return Instance;
        }
    }
}
