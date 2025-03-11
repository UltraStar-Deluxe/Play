using Unity.Netcode;
using UnityEngine;

namespace CommonOnlineMultiplayer
{
    public static class NetworkManagerInitialization
    {
        public static void InitNetworkManagerSingleton()
        {
            if (NetworkManager.Singleton != null)
            {
                return;
            }

            Debug.Log("Initializing NetworkManager.Singleton");
            FindNetworkManager().SetSingleton();
        }

        private static NetworkManager FindNetworkManager()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager != null)
            {
                return networkManager;
            }

            networkManager = GameObject.FindObjectOfType<NetworkManager>();
            if (networkManager != null)
            {
                return networkManager;
            }

            throw new NetworkManagerNotFoundException();
        }
    }
}
