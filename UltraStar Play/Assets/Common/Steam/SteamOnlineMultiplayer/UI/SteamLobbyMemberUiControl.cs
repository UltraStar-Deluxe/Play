using System;
using CommonOnlineMultiplayer;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

namespace SteamOnlineMultiplayer
{
    public class SteamLobbyMemberUiControl : LobbyMemberUiControl
    {
        private SteamLobbyMember steamLobbyMember;

        public override void OnInjectionFinished()
        {
            steamLobbyMember = lobbyMember as SteamLobbyMember;
            if (steamLobbyMember == null)
            {
                throw new Exception("SteamLobbyMember not set");
            }

            base.OnInjectionFinished();
        }

        protected override async void UpdateImage()
        {
            Image itemElement = playerProfileImageChooser.ItemImage;

            itemElement.SetBorderRadius(Length.Percent(50));
            try
            {
                Texture2D texture = await SteamAvatarImageUtils.GetAvatarTextureAsync(steamLobbyMember.SteamId);
                itemElement.image = texture;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Debug.LogError($"Failed to get avatar image of Steam user '{steamLobbyMember.DisplayName}' with id {steamLobbyMember.SteamId}");
                itemElement.style.backgroundImage = new StyleBackground(PlayerProfileImageManager.Instance.fallbackPlayerProfileImage);
                itemElement.style.unityBackgroundImageTintColor = new StyleColor(ColorGenerationUtils.FromString(steamLobbyMember.DisplayName));
            }
        }
    }
}
