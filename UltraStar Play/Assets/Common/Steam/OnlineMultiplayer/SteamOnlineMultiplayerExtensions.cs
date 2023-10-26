using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using Steamworks.ServerList;
using Unity.Netcode;
using UnityEngine;

namespace SteamOnlineMultiplayer
{
    public static class SteamOnlineMultiplayerExtensions
    {
        private static string GameId => SteamManager.MelodyManiaSteamAppId.ToString();

        public enum OverlayType : byte
        {
            Friends,
            Community,
            Players,
            Settings,
            OfficialGameGroup,
            Stats,
            Achievements
        }

        [Serializable]
        public struct LobbyConfig
        {
            public string name;
            public bool joinable;
            public ESteamLobbyVisibility visibility;
            public byte maxMembers;
        }

        public static void SetVisibility(this Lobby lobby, ESteamLobbyVisibility visibility)
        {
            switch (visibility)
            {
                case ESteamLobbyVisibility.Public:
                    lobby.SetPublic();
                    break;
                case ESteamLobbyVisibility.Private:
                    lobby.SetPrivate();
                    break;
                case ESteamLobbyVisibility.FriendsOnly:
                    lobby.SetFriendsOnly();
                    break;
                case ESteamLobbyVisibility.Invisible:
                    lobby.SetInvisible();
                    break;
            }
        }

        /**
         * Get shareable link for lobby.
         * The link is a URI that is intended to be opened by a web browser.
         * The browser can forward the URI to the Steam client, which then automatically connects the game.
         */
        public static string GetShareableUri(this Lobby lobby) =>
            $"steam://joinlobby/{SteamClient.AppId}/{lobby.Id}/{SteamClient.SteamId}";

        private const int CodeAppendPos = 4;

        /**
         * Get shareable code for lobby.
         * The code is in base16 in the format XXXX-XXXX-XXXX-XXXX.
         */
        public static string GetShareableCode(this Lobby lobby)
        {
            string input = $"{lobby.Id:X16}";
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < input.Length; i++)
            {
                if (i % CodeAppendPos == 0 && i > 0)
                    builder.Append('-');

                builder.Append(input[i]);
            }

            return builder.ToString().Trim();
        }

        public static IEnumerable<Friend> GetFriends()
        {
            if (!SteamManager.Instance.IsConnectedToSteam)
                return default;

            return SteamFriends.GetFriends();
        }

        public static async Task<Texture2D> GetAvatarTextureAsync(this SteamId steamId)
        {
            var image = await SteamFriends.GetLargeAvatarAsync(steamId);
            return await GetSteamImageAsTextureAsync(image ?? default);
        }

        public static async Task<Texture2D> GetSteamImageAsTextureAsync(this Image image)
        {
            return await Task.Run(() =>
            {
                var texture = new Texture2D((int)image.Width, (int)image.Width, TextureFormat.RGBA32, mipChain: false,
                    linear: true);

                texture.LoadRawTextureData(image.Data);
                texture.Apply();

                return texture;
            });
        }

        /**
         * Gets lobbies in the same immediate region
         */
        public static async Task<Lobby[]> GetLobbiesAsync()
        {
            if (!SteamManager.Instance.IsConnectedToSteam)
            {
                Debug.LogWarning("Failed to find lobbies. Steam is not running");
                return await Task.FromResult(new Lobby[0]);
            }

            return await SteamMatchmaking.LobbyList
                .WithKeyValue("game", GameId)
                .FilterDistanceClose()
                .WithMaxResults(max: 20)
                .RequestAsync();
        }

        /**
         * Gets servers in the same immediate region
         */
        public static async Task<List<ServerInfo>> GetListOfServersAsync()
        {
            List<ServerInfo> servers = new List<ServerInfo>();

            using Internet list = new Internet();

            await list.RunQueryAsync();
            await Task.Yield();

            servers.AddRange(list.Responsive);

            return servers;
        }

        /**
         * Will begin to download a item from Steam workshop
         */
        public static async Task<bool> DownloadItemAsync(this PublishedFileId fileId, Action<float> onProgress) =>
            await SteamUGC.DownloadAsync(fileId, progress: onProgress, ct: workshopItemToken);

        /**
         * Writes a file to the cloud (<b>NOTE:</b> Max size is 100 MiB)
         */
        public static void WriteFileToCloudAsync(string filename, byte[] data) =>
            SteamRemoteStorage.FileWrite(filename, data);

        /**
         * Reads a file from the cloud
         */
        public static byte[] ReadFileFromCloudAsync(string filename) => SteamRemoteStorage.FileRead(filename);

        private static CancellationToken workshopItemToken;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
#if UNITY_EDITOR
            Debug.unityLogger.logEnabled = true;
#else
Debug.unityLogger.logEnabled = Debug.isDebugBuild || false;
#endif
            if (NetworkManager.Singleton == null || !SteamManager.Instance.IsConnectedToSteam)
                return;

            SteamFriends.ClearRichPresence();

            Application.quitting += Shutdown;

            var source = new CancellationTokenSource();
            workshopItemToken = source.Token;
        }

        private static void Shutdown()
        {
            Application.quitting -= Shutdown;
        }
    }
}
