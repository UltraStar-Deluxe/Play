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
                && lobbyMemberPlayerProfile.IsRemote)
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

        public static string GetPlayerDisplayName(OnlineMultiplayerManager onlineMultiplayerManager, NamedMessage message)
        {
            if (onlineMultiplayerManager == null
                || onlineMultiplayerManager.LobbyMemberManager == null)
            {
                return "Unknown Player";
            }

            LobbyMember lobbyMember = onlineMultiplayerManager.LobbyMemberManager.GetLobbyMember(message.SenderNetcodeClientId);
            if (lobbyMember == null)
            {
                return "Unknown Player";
            }

            return lobbyMember.DisplayName;
        }

        public static bool IsLocalPlayerProfile(PlayerProfile playerProfile)
        {
            return playerProfile is not LobbyMemberPlayerProfile lobbyMemberPlayerProfile
                   || lobbyMemberPlayerProfile.IsLocal;
        }

        public static bool IsRemotePlayerProfile(PlayerProfile playerProfile)
        {
            return !IsLocalPlayerProfile(playerProfile);
        }
    }
}
