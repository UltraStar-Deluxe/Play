using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

namespace SteamOnlineMultiplayer
{
    public static class SteamOnlineMultiplayerUtils
    {
        /**
         * Get shareable link for lobby.
         * The link is a URI that is intended to be opened by a web browser.
         * The browser can forward the URI to the Steam client, which then automatically connects the game.
         */
        public static string GetShareableUri(Lobby lobby) =>
            $"steam://joinlobby/{SteamClient.AppId}/{lobby.Id}/{SteamClient.SteamId}";

        private const int CodeAppendPos = 4;

        /**
         * Get shareable code for lobby.
         * The code is in base16 in the format XXXX-XXXX-XXXX-XXXX.
         */
        public static string GetShareableCode(Lobby lobby)
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

        public static async Task<Texture2D> GetAvatarTextureAsync(SteamId steamId)
        {
            Image? image = await SteamFriends.GetLargeAvatarAsync(steamId);
            return await GetSteamImageAsTextureAsync(image ?? default);
        }

        public static async Task<Texture2D> GetSteamImageAsTextureAsync(Image image)
        {
            return await Task.Run(() =>
            {
                Texture2D texture = new Texture2D((int)image.Width, (int)image.Width, TextureFormat.RGBA32, mipChain: false,
                    linear: true);

                texture.LoadRawTextureData(image.Data);
                texture.Apply();

                return texture;
            });
        }

        /**
         * Will begin to download a item from Steam workshop
         */
        public static async Task<bool> DownloadItemAsync(PublishedFileId fileId, Action<float> onProgress, CancellationToken cancellationToken)
        {
            return await SteamUGC.DownloadAsync(fileId, progress: onProgress, ct: cancellationToken);
        }

        /**
         * Writes a file to the cloud (<b>NOTE:</b> Max size is 100 MiB)
         */
        public static void WriteFileToCloudAsync(string filename, byte[] data)
        {
            SteamRemoteStorage.FileWrite(filename, data);
        }

        /**
         * Reads a file from the cloud
         */
        public static byte[] ReadFileFromCloudAsync(string filename)
        {
            return SteamRemoteStorage.FileRead(filename);
        }
    }
}
