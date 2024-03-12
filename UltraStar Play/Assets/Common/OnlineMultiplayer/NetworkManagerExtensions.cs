using Unity.Netcode;
using UnityEngine;

namespace CommonOnlineMultiplayer
{
    public static class NetworkManagerExtensions
    {
        public static void ShutdownIfConnectedClient(this NetworkManager networkManager, string logPrefix)
        {
            if (networkManager.IsConnectedClient)
            {
                Debug.Log($"{logPrefix}. Thus, shutting down NetworkManger");
                networkManager.Shutdown();
            }
            else if (networkManager.IsListening)
            {
                Debug.Log($"{logPrefix}. But only listening, not yet connected as Netcode client. Thus, shutting down NetworkManger.");
                networkManager.Shutdown();
            }
            else
            {
                Debug.Log($"{logPrefix}. But not connected and not listening for connections, so not shutting down NetworkManger");
            }
        }
    }
}
