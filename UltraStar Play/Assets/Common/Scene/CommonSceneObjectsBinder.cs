using System.Collections.Generic;
using CommonOnlineMultiplayer;
using Netcode.Transports.Facepunch;
using PrimeInputActions;
using SimpleHttpServerForUnity;
using SteamOnlineMultiplayer;
using UniInject;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

public class CommonSceneObjectsBinder : MonoBehaviour, IBinder
{
    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new();
        bb.BindExistingInstance(ApplicationManager.Instance);
        bb.BindExistingInstance(SceneNavigator.Instance);
        bb.BindExistingInstance(SceneRecipeManager.Instance);
        bb.BindExistingInstance(SettingsManager.Instance);
        bb.BindExistingInstance(SongMetaManager.Instance);
        bb.BindExistingInstance(SongIssueManager.Instance);
        bb.BindExistingInstance(CursorManager.Instance);
        bb.BindExistingInstance(UiManager.Instance);
        bb.BindExistingInstance(MidiManager.Instance);
        bb.BindExistingInstance(ImageManager.Instance);
        bb.BindExistingInstance(AudioManager.Instance);
        bb.BindExistingInstance(SfxManager.Instance);
        bb.BindExistingInstance(TranslationManager.Instance);
        bb.BindExistingInstance(ContextMenuPopupManager.Instance);
        bb.BindExistingInstance(WebCamManager.Instance);
        bb.BindExistingInstance(RenderTextureManager.Instance);
        bb.BindExistingInstance(DontDestroyOnLoadManager.Instance);
        bb.BindExistingInstance(PlaylistManager.Instance);
        bb.BindExistingInstance(StatisticsManager.Instance);
        bb.BindExistingInstance(InputManager.Instance);
        bb.BindExistingInstance(BackgroundMusicManager.Instance);
        bb.BindExistingInstance(VfxManager.Instance);
        bb.BindExistingInstance(InGameDebugConsoleManager.Instance);
        bb.BindExistingInstance(BackgroundLightManager.Instance);
        bb.BindExistingInstance(DefaultFocusableNavigator.Instance);
        bb.BindExistingInstance(MicSampleRecorderManager.Instance);
        bb.BindExistingInstance(AchievementEventStream.Instance);
        bb.BindExistingInstance(WebViewManager.Instance);
        bb.BindExistingInstance(SongMediaFileConversionManager.Instance);
        bb.BindExistingInstance(ModManager.Instance);
        bb.BindExistingInstance(RuntimeUiInspectionManager.Instance);
        bb.BindExistingInstance(VlcManager.Instance);
        bb.Bind(typeof(FocusableNavigator)).ToExistingInstance(DefaultFocusableNavigator.Instance);

        // Steam
        bb.BindExistingInstance(SteamManager.Instance);
        bb.BindExistingInstance(SteamAchievementManager.Instance);
        bb.BindExistingInstance(SteamWorkshopManager.Instance);

        // Online Multiplayer
        bb.BindExistingInstance(OnlineMultiplayerManager.Instance);
        bb.BindExistingInstance(OnlineMultiplayerBackendManager.Instance);

        // Netcode online multiplayer (direct connection without Steam lobby)
        bb.BindExistingInstance(NetcodeLobbyManager.Instance);
        bb.BindExistingInstance(NetcodeLobbyMemberManager.Instance);
        bb.BindExistingInstance(NetcodeOnlineMultiplayerBackendConfigurator.Instance);

        // Steam online multiplayer
        bb.BindExistingInstance(SteamLobbyManager.Instance);
        bb.BindExistingInstance(SteamLobbyMemberManager.Instance);
        bb.BindExistingInstance(SteamOnlineMultiplayerBackendConfigurator.Instance);
        bb.BindExistingInstance(DontDestroyOnLoadManager.Instance.FindComponentOrThrow<FacepunchTransport>());

        if (NetworkManager.Singleton == null)
        {
            FindOrCreateNetworkManager().SetSingleton();
        }
        bb.BindExistingInstance(NetworkManager.Singleton);

        bb.BindExistingInstance(SpeechRecognitionManager.Instance);
        bb.BindExistingInstance(AudioSeparationManager.Instance);
        bb.BindExistingInstance(PitchDetectionManager.Instance);
        bb.BindExistingInstance(SongQueueManager.Instance);
        bb.BindExistingInstance(JobManager.Instance);   bb.BindExistingInstance(UltraStarPlaySceneChangeAnimationControl.Instance);
        bb.BindExistingInstance(ThemeManager.Instance);
        bb.BindExistingInstance(GlobalInputControl.Instance);
        bb.BindExistingInstance(VolumeControl.Instance);
        bb.Bind(typeof(UltraStarPlayInputManager)).ToExistingInstance(UltraStarPlayInputManager.Instance);
        bb.BindExistingInstance(HttpServer.Instance);
        bb.BindExistingInstance(HttpServer.Instance as UltraStarPlayHttpServer);
        bb.Bind(typeof(IServerSideCompanionClientManager)).ToExistingInstance(ServerSideCompanionClientManager.Instance);
        bb.BindExistingInstance(ServerSideCompanionClientManager.Instance);

        EventSystem eventSystem = GameObjectUtils.FindComponentWithTag<EventSystem>("EventSystem");
        bb.BindExistingInstance(eventSystem);

        UIDocument uiDocument = UIDocumentUtils.FindUIDocumentOrThrow();
        bb.BindExistingInstance(uiDocument);
        bb.BindExistingInstance(new PanelHelper(uiDocument));

        // Lazy binding of settings, because they are not needed in every scene and loading the settings takes time.
        bb.Bind(typeof(ISettings)).ToExistingInstance(() => SettingsManager.Instance.Settings);
        bb.BindExistingInstanceLazy(() => SettingsManager.Instance.Settings);
        bb.BindExistingInstanceLazy(() => SettingsManager.Instance.Settings.PartyModeSettings);
        bb.BindExistingInstanceLazy(() => SettingsManager.Instance.NonPersistentSettings);
        bb.BindExistingInstanceLazy(() => StatisticsManager.Instance.Statistics);

        return bb.GetBindings();
    }

    private NetworkManager FindOrCreateNetworkManager()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager != null)
        {
            return networkManager;
        }

        networkManager = FindObjectOfType<NetworkManager>();
        if (networkManager != null)
        {
            return networkManager;
        }

        GameObject networkManagerGameObject = new GameObject();
        networkManagerGameObject.name = "NetworkManager-RuntimeCreated";
        networkManager = networkManagerGameObject.AddComponent<NetworkManager>();
        return networkManager;

    }
}
