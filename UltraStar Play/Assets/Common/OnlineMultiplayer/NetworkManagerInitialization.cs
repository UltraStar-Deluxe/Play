using Unity.Netcode;
using UnityEngine;

namespace CommonOnlineMultiplayer
{
    public static class NetworkManagerInitialization
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void StaticInit()
        {
            InitNetworkManagerSingleton();
        }

        public static void InitNetworkManagerSingleton()
        {
            if (NetworkManager.Singleton != null)
            {
                return;
            }

            Debug.Log("Initializing NetworkManager.Singleton");
            FindOrCreateNetworkManager().SetSingleton();
        }

        private static NetworkManager FindOrCreateNetworkManager()
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

            GameObject networkManagerGameObject = new GameObject();
            networkManagerGameObject.name = "NetworkManager-RuntimeCreated";
            networkManager = networkManagerGameObject.AddComponent<NetworkManager>();
            return networkManager;
        }
    }
}
