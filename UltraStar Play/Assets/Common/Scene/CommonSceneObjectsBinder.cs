using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.EventSystems;

public class CommonSceneObjectsBinder : MonoBehaviour, IBinder
{
    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new BindingBuilder();
        bb.BindExistingInstance(ApplicationManager.Instance);
        bb.BindExistingInstance(SceneNavigator.Instance);
        bb.BindExistingInstance(SettingsManager.Instance);
        bb.BindExistingInstance(SongMetaManager.Instance);
        bb.BindExistingInstance(ThemeManager.Instance);
        bb.BindExistingInstance(CursorManager.Instance);
        bb.BindExistingInstance(UiManager.Instance);
        bb.BindExistingInstance(MidiManager.Instance);
        bb.BindExistingInstance(AudioManager.Instance);
        bb.BindExistingInstance(I18NManager.Instance);
        bb.BindExistingInstance(PlaylistManager.Instance);
        bb.BindExistingInstance(StatsManager.Instance);
        bb.BindExistingInstance(CoroutineManager.Instance);
        bb.BindExistingInstance(InputManager.Instance);

        EventSystem eventSystem = GameObjectUtils.FindComponentWithTag<EventSystem>("EventSystem");
        bb.BindExistingInstance(eventSystem);

        // Lazy binding of settings, because they are not needed in every scene and loading the settings takes time.
        bb.BindExistingInstanceLazy(() => SettingsManager.Instance.Settings);
        bb.BindExistingInstanceLazy(() => StatsManager.Instance.Statistics);

        return bb.GetBindings();
    }
}
