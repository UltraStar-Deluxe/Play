using System.Collections.Generic;
using System.Linq;
using UniInject;

namespace CommonOnlineMultiplayer
{
    public class OnlineMultiplayerBackendManager : AbstractSingletonBehaviour, INeedInjection
    {
        public static OnlineMultiplayerBackendManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<OnlineMultiplayerBackendManager>();

        public OnlineMultiplayerBackend CurrentBackend => onlineMultiplayerBackends
            .FirstOrDefault(it => it.Backend == settings.EOnlineMultiplayerBackend);

        private readonly List<OnlineMultiplayerBackend> onlineMultiplayerBackends = new();

        [Inject]
        private Settings settings;

        protected override object GetInstance()
        {
            return Instance;
        }

        public void AddBackend(OnlineMultiplayerBackend backend)
        {
            onlineMultiplayerBackends.Add(backend);
        }
    }
}
