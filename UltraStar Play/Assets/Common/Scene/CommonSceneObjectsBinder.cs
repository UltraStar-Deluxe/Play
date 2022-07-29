using System.Collections.Generic;
using PrimeInputActions;
using ProTrans;
using SceneChangeAnimations;
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
        bb.BindExistingInstance(SettingsManager.Instance);
        bb.BindExistingInstance(SongMetaManager.Instance);
        bb.BindExistingInstance(CursorManager.Instance);
        bb.BindExistingInstance(UiManager.Instance);
        bb.BindExistingInstance(MidiManager.Instance);
        bb.BindExistingInstance(AudioManager.Instance);
        bb.BindExistingInstance(TranslationManager.Instance);
        bb.BindExistingInstance(ContextMenuPopupManager.Instance);
        bb.BindExistingInstance(PlaylistManager.Instance);
        bb.BindExistingInstance(StatsManager.Instance);
        bb.BindExistingInstance(CoroutineManager.Instance);
        bb.BindExistingInstance(InputManager.Instance);
        bb.BindExistingInstance(BackgroundMusicManager.Instance);
        bb.BindExistingInstance(UltraStarPlaySceneChangeAnimationControl.Instance);
        bb.BindExistingInstance(UltraStarPlaySceneChangeAnimationControl.Instance);
        bb.Bind(typeof(SceneChangeAnimationControl)).ToExistingInstance(UltraStarPlayInputManager.Instance);
        bb.Bind(typeof(UltraStarPlayInputManager)).ToExistingInstance(UltraStarPlayInputManager.Instance);
        bb.BindExistingInstance(HttpServer.Instance);
        bb.Bind(typeof(IServerSideConnectRequestManager)).ToExistingInstance(ServerSideConnectRequestManager.Instance);
        bb.BindExistingInstance(ServerSideConnectRequestManager.Instance);

        EventSystem eventSystem = GameObjectUtils.FindComponentWithTag<EventSystem>("EventSystem");
        bb.BindExistingInstance(eventSystem);

        // Lazy binding of UIDocument, because it does not exist in every scene (yet)
        bb.BindExistingInstanceLazy(() => GetUiDocument());
        bb.BindExistingInstanceLazy(() => new PanelHelper(GetUiDocument()));

        // Lazy binding of settings, because they are not needed in every scene and loading the settings takes time.
        bb.Bind(typeof(ISettings)).ToExistingInstance(() => SettingsManager.Instance.Settings);
        bb.BindExistingInstanceLazy(() => SettingsManager.Instance.Settings);
        bb.BindExistingInstanceLazy(() => StatsManager.Instance.Statistics);

        return bb.GetBindings();
    }

    private static UIDocument GetUiDocument()
    {
        GameObject uiDocGameObject = GameObject.FindWithTag("UIDocument");
        if (uiDocGameObject != null)
        {
            return uiDocGameObject.GetComponent<UIDocument>();
        }
        return null;
    }
}
