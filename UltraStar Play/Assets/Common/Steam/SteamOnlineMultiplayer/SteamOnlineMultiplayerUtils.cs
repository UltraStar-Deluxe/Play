using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using Steamworks.ServerList;
using UnityEngine;

namespace SteamOnlineMultiplayer
{
    public static class SteamOnlineMultiplayerUtils
    {
        public static IEnumerable<Friend> GetFriends()
        {
            if (!SteamManager.Instance.IsConnectedToSteam)
                return default;

            return SteamFriends.GetFriends();
        }

        /**
         * Gets lobbies in the same immediate region
         */
        public static async Task<Lobby[]> GetLobbiesAsync()
        {
            if (!SteamManager.Instance.IsConnectedToSteam)
            {
                Debug.LogWarning("Failed to find lobbies. Steam is not running");
                return await Task.FromResult(Array.Empty<Lobby>());
            }

            return await SteamMatchmaking.LobbyList
                .WithKeyValue("appId", SteamConstants.MelodyManiaSteamAppId.ToString())
                .FilterDistanceClose()
                .WithMaxResults(max: 20)
                .RequestAsync()
                   ?? Array.Empty<Lobby>();
        }

        /**
         * Gets servers in the same immediate region
         */
        public static async Task<List<ServerInfo>> GetListOfServersAsync()
        {
            List<ServerInfo> servers = new List<ServerInfo>();

            using Internet list = new Internet();

            await list.RunQueryAsync();

            servers.AddRange(list.Responsive);
            return servers;
        }
    }
}
