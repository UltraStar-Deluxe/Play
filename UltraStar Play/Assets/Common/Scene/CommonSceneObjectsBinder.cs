using System.Collections.Generic;
using PrimeInputActions;
using ProTrans;
using SimpleHttpServerForUnity;
using UniInject;
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
        bb.BindExistingInstance(CursorManager.Instance);
        bb.BindExistingInstance(UiManager.Instance);
        bb.BindExistingInstance(MidiManager.Instance);
        bb.BindExistingInstance(ImageManager.Instance);
        bb.BindExistingInstance(AudioManager.Instance);
        bb.BindExistingInstance(TranslationManager.Instance);
        bb.BindExistingInstance(UltraStarPlayTranslationManager.Instance);
        bb.BindExistingInstance(ContextMenuPopupManager.Instance);
        bb.BindExistingInstance(WebCamManager.Instance);
        bb.BindExistingInstance(RenderTextureManager.Instance);
        bb.BindExistingInstance(DontDestroyOnLoadManager.Instance);
        bb.BindExistingInstance(PlaylistManager.Instance);
        bb.BindExistingInstance(StatsManager.Instance);
        bb.BindExistingInstance(InputManager.Instance);
        bb.BindExistingInstance(BackgroundMusicManager.Instance);
        bb.BindExistingInstance(VfxManager.Instance);
        bb.BindExistingInstance(InGameDebugConsoleManager.Instance);
        bb.BindExistingInstance(BackgroundLightManager.Instance);
        bb.BindExistingInstance(DefaultFocusableNavigator.Instance);
        bb.BindExistingInstance(SteamManager.Instance);
        bb.BindExistingInstance(MicSampleRecorderManager.Instance);
        bb.BindExistingInstance(AchievementEventStream.Instance);
        bb.BindExistingInstance(WebViewManager.Instance);
        bb.BindExistingInstance(SongMediaFileConversionManager.Instance);
        bb.BindExistingInstance(ModManager.Instance);
        bb.BindExistingInstance(RuntimeUiInspectionManager.Instance);
        bb.BindExistingInstance(VlcManager.Instance);
        bb.Bind(typeof(FocusableNavigator)).ToExistingInstance(DefaultFocusableNavigator.Instance);

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
        bb.Bind(typeof(IServerSideConnectRequestManager)).ToExistingInstance(ServerSideConnectRequestManager.Instance);
        bb.BindExistingInstance(ServerSideConnectRequestManager.Instance);

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
        bb.BindExistingInstanceLazy(() => StatsManager.Instance.Statistics);

        return bb.GetBindings();
    }
}
