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
    public static class SteamAvatarImageUtils
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void StaticInit()
        {
            steamIdToAvatarTextureCache.ForEach(entry => GameObject.Destroy(entry.Value));
            steamIdToAvatarTextureCache.Clear();
        }

        private static readonly Dictionary<SteamId, Texture2D> steamIdToAvatarTextureCache = new();

        public static async Awaitable<Texture2D> GetAvatarTextureAsync(SteamId steamId)
        {
            if (steamIdToAvatarTextureCache.TryGetValue(steamId, out Texture2D cachedTexture))
            {
                return cachedTexture;
            }

            await Awaitable.BackgroundThreadAsync();
            Image? steamImage = await SteamFriends.GetLargeAvatarAsync(steamId);
            await Awaitable.MainThreadAsync();

            if (!steamImage.HasValue)
            {
                throw new OnlineMultiplayerException($"No avatar image found for player with Steam id {steamId}");
            }

            Texture2D texture = CreateTextureFromSteamImage(steamImage.Value);
            steamIdToAvatarTextureCache[steamId] = texture;
            return texture;
        }

        private static Texture2D CreateTextureFromSteamImage(Image image)
        {
            Texture2D texture = new Texture2D((int)image.Width, (int)image.Height, TextureFormat.RGBA32, mipChain: false, linear: true);
            texture.LoadRawTextureData(image.Data);
            TextureUtils.FlipTextureVertically(texture);
            texture.Apply();
            return texture;
        }
    }
}
