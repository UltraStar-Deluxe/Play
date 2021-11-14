using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class TogglePlayPauseAudioButton : MonoBehaviour, INeedInjection
{
    [Inject]
    private SongEditorSceneController songEditorSceneController;

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private Button button;

    void Start()
    {
        button.OnClickAsObservable().Subscribe(_ => songEditorSceneController.ToggleAudioPlayPause());
    }
}
