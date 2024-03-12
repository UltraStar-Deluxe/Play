using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommonOnlineMultiplayer;
using Steamworks;
using Steamworks.Data;
using UniRx;
using UnityEngine;
using Util;

namespace SteamOnlineMultiplayer
{
    public static class SteamOnlineMultiplayerUtils
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void StaticInit()
        {
            steamIdToAvatarTextureCache.ForEach(entry => GameObject.Destroy(entry.Value));
            steamIdToAvatarTextureCache.Clear();
        }

        private static readonly Dictionary<SteamId, Texture2D> steamIdToAvatarTextureCache = new();

        public static IObservable<Texture2D> GetAvatarTextureAsObservable(SteamId steamId)
        {
            if (steamIdToAvatarTextureCache.TryGetValue(steamId, out Texture2D cachedTexture))
            {
                return Observable.Return(cachedTexture);
            }

            return ObservableUtils.RunOnNewTaskAsObservable(async () =>
                {
                    return await SteamFriends.GetLargeAvatarAsync(steamId);
                })
                .ObserveOnMainThread()
                .Select(steamImage =>
                {
                    if (!steamImage.HasValue)
                    {
                        throw new OnlineMultiplayerException($"No avatar image found for player with Steam id {steamId}");
                    }

                    Texture2D texture = CreateTextureFromSteamImage(steamImage.Value);
                    steamIdToAvatarTextureCache[steamId] = texture;
                    return texture;
                });
        }

        private static Texture2D CreateTextureFromSteamImage(Image image)
        {
            Texture2D texture = new Texture2D((int)image.Width, (int)image.Height, TextureFormat.RGBA32, mipChain: false, linear: true);
            texture.LoadRawTextureData(image.Data);
            TextureUtils.FlipTextureVertically(texture);
            texture.Apply();
            return texture;
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
