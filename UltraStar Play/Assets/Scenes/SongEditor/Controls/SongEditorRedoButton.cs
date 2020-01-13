using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorRedoButton : MonoBehaviour, INeedInjection
{
    [Inject]
    private SongEditorHistoryManager historyManager;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private Button button;

    void Start()
    {
        button.OnClickAsObservable().Subscribe(_ => historyManager.Redo());
    }
}
