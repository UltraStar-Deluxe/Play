using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;

namespace CommonOnlineMultiplayer
{
    public class StartSongRequestManager : AbstractOnlineMultiplayerRequestManager
    {
        public static StartSongRequestManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<StartSongRequestManager>();

        [Inject]
        private SceneNavigator sceneNavigator;

        [Inject]
        private SongMetaManager songMetaManager;

        [Inject]
        private Settings settings;

        [Inject]
        private NonPersistentSettings nonPersistentSettings;

        [Inject]
        private ThemeManager themeManager;

        [Inject]
        private ServerSideCompanionClientManager serverSideCompanionClientManager;

        protected override object GetInstance()
        {
            return Instance;
        }

        protected override void InitOnlineMultiplayerRequestHandlers()
        {
            onlineMultiplayerManager.MessagingControl.RegisterNamedMessageHandler(
                nameof(StartSingSceneRequestDto),
                response =>
                {
                    if (onlineMultiplayerManager.IsHost)
                    {
                        // This request is only sent by the host, which is this lobby member, so nothing to do here.
                        return;
                    }

                    if (onlineMultiplayerManager.OwnLobbyMemberPlayerProfile == null)
                    {
                        Debug.LogError("Failed to start sing scene from request because this lobby member has no corresponding player profile.");
                        return;
                    }

                    StartSingSceneRequestDto requestDto = FastBufferReaderUtils.ReadJsonValuePacked<StartSingSceneRequestDto>(response.MessagePayload);

                    MicProfile micProfile = GetOwnLobbyMemberMicProfile();
                    SingSceneData singSceneData = NetcodeMessageDtoConverterUtils.FromDto(requestDto.SingSceneDataDto, songMetaManager);
                    singSceneData.SingScenePlayerData = new()
                    {
                        SelectedPlayerProfiles = nonPersistentSettings.LobbyMemberPlayerProfiles
                            .Cast<PlayerProfile>()
                            .ToList(),
                        PlayerProfileToMicProfileMap = new Dictionary<PlayerProfile, MicProfile>()
                        {
                            { onlineMultiplayerManager.OwnLobbyMemberPlayerProfile, micProfile },
                        },
                    };
                    sceneNavigator.LoadScene(EScene.SingScene, singSceneData);
                })
                .AddTo(gameObject);
        }

        private MicProfile GetOwnLobbyMemberMicProfile()
        {
            // Try use mic that was last used.
            List<MicProfile> availableMicProfiles = GetAvailableMicProfiles();
            if (onlineMultiplayerManager.OwnLobbyMemberPlayerProfile != null
                && settings.PlayerProfileNameToLastUsedMicProfile.TryGetValue(onlineMultiplayerManager.OwnLobbyMemberPlayerProfile.Name, out MicProfileReference micProfileReference))
            {
                MicProfile result = availableMicProfiles.FirstOrDefault(it => it.Equals(micProfileReference));
                if (result != null)
                {
                    return result;
                }
            }

            // Use any available mics.
            return availableMicProfiles.FirstOrDefault();
        }

        private List<MicProfile> GetAvailableMicProfiles()
        {
            return SettingsUtils.GetAvailableMicProfiles(settings, themeManager, serverSideCompanionClientManager);
        }
    }
}
