using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace CommonOnlineMultiplayer
{
    public static class CommonOnlineMultiplayerUtils
    {
        public static Color32 GetPlayerColor(PlayerProfile playerProfile, MicProfile micProfile)
        {
            if (playerProfile is LobbyMemberPlayerProfile lobbyMemberPlayerProfile
                && lobbyMemberPlayerProfile.UnityNetcodeClientId != NetworkManager.Singleton.LocalClientId)
            {
                return ColorGenerationUtils.FromString(playerProfile.Name);
            }
            else if (micProfile != null)
            {
                return micProfile.Color;
            }

            return Colors.clearBlack;
        }

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

        public static ClientRpcParams CreateSendToClientRpcParams(IReadOnlyList<ulong> targetNetcodeClientIds)
        {
            return new ClientRpcParams()
            {
                Send = new ClientRpcSendParams()
                {
                    TargetClientIds = targetNetcodeClientIds,
                }
            };
        }
    }
}
