using System;
using System.Threading;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

namespace SteamOnlineMultiplayer
{
    public static class SteamOnlineMultiplayerUtils
    {
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
         * Will begin to download an item from Steam workshop
         */
        public static async Task<bool> DownloadWorkshopItemAsync(PublishedFileId fileId, Action<float> onProgress, CancellationToken cancellationToken)
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
