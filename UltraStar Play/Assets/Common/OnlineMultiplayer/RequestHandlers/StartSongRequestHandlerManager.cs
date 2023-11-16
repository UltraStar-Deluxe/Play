using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;

namespace CommonOnlineMultiplayer
{
    public class StartSongRequestHandlerManager : AbstractSingletonBehaviour, INeedInjection
    {
        public static StartSongRequestHandlerManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<StartSongRequestHandlerManager>();

        [Inject]
        private SceneNavigator sceneNavigator;

        [Inject]
        private SongMetaManager songMetaManager;

        [Inject]
        private OnlineMultiplayerManager onlineMultiplayerManager;

        [Inject]
        private Settings settings;

        [Inject]
        private NonPersistentSettings nonPersistentSettings;

        [Inject]
        private ThemeManager themeManager;

        [Inject]
        private ServerSideConnectRequestManager serverSideConnectRequestManager;

        protected override object GetInstance()
        {
            return Instance;
        }

        protected override void StartSingleton()
        {
            InitOnlineMultiplayerRequestHandlers();
        }

        private void InitOnlineMultiplayerRequestHandlers()
        {
            NetcodeRequestHandler<StartSingSceneRequestDto> suggestSongRequestHandler = new NetcodeRequestHandler<StartSingSceneRequestDto>(
                ENetcodeMessageType.StartSongRequest,
                0,
                (requestDto, senderNetcodeClientId) =>
                {
                    if (onlineMultiplayerManager.IsServer)
                    {
                        // This request is only sent by the host, which is this lobby member, so nothing to do here.
                        return EmptyNetcodeResponseDto.Instance;
                    }

                    if (onlineMultiplayerManager.OwnLobbyMemberPlayerProfile == null)
                    {
                        Debug.LogError("Failed to start sing scene from request because this lobby member has no corresponding player profile.");
                        return EmptyNetcodeResponseDto.Instance;
                    }

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
                    return EmptyNetcodeResponseDto.Instance;
                });
            onlineMultiplayerManager.NetcodeRequestHandlerRegistry.AddRequestHandler(suggestSongRequestHandler);
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
            return SettingsUtils.GetAvailableMicProfiles(settings, themeManager, serverSideConnectRequestManager);
        }
    }
}
