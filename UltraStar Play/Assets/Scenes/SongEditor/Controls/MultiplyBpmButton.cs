using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MultiplyBpmButton : MonoBehaviour, INeedInjection
{
    [Range(2, 3)]
    public int factor = 2;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private Button button;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private NoteArea noteArea;

    [Inject]
    private SongEditorSceneController songEditorSceneController;

    void Start()
    {
        button.OnClickAsObservable().Subscribe(_ => OnButtonClicked());
    }

    private void OnButtonClicked()
    {
        ChangeBpmAction.MultiplyBpm(songMeta, factor);
        songEditorSceneController.OnNotesChanged();
        noteArea.SetViewportHorizontal(noteArea.ViewportX, noteArea.ViewportWidth);
        FindObjectOfType<BpmText>()?.SetBpm(songMeta.Bpm);
    }
}
