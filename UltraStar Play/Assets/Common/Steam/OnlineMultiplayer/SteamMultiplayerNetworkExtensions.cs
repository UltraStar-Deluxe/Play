using Unity.Netcode;
using UnityEngine;

namespace SteamOnlineMultiplayer
{
    public static class SteamMultiplayerNetworkExtensions
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StaticInit()
        {
            SteamMultiplayerNetworkManager.OnDisconnectReceived += OnDisconnectReceived;
            Application.quitting += Shutdown;
        }

        /**
         * Checks if we are the owner
         * NOTE: If NetworkManager.Singleton is null, then owner is true (helpful for single player)
         */
        public static bool IsOwner(this NetworkBehaviour obj) => NetworkManager.Singleton == null || obj.IsOwner;

        private static void Shutdown()
        {
            Application.quitting -= Shutdown;
            SteamMultiplayerNetworkManager.OnDisconnectReceived -= OnDisconnectReceived;
        }

        private static void OnDisconnectReceived(ConnectStatus status) => Debug.Log($"Disconnect: {status}");
    }
}
