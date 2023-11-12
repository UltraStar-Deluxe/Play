using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace CommonOnlineMultiplayer
{
    public static class CommonOnlineMultiplayerUtils
    {
        public static void ConfigureUnityTransport(NetworkManager networkManager, Settings settings)
        {
            if (networkManager.NetworkConfig.NetworkTransport is not UnityTransport unityTransport)
            {
                unityTransport = networkManager.GetComponentInChildren<UnityTransport>();
                if (unityTransport == null)
                {
                    throw new OnlineMultiplayerException("Unable to find UnityTransport component in NetworkManager");
                }

                networkManager.NetworkConfig.NetworkTransport = unityTransport;
            }

            unityTransport.SetConnectionData(settings.UnityTransportIpAddress, settings.UnityTransportPort);
        }
    }
}
