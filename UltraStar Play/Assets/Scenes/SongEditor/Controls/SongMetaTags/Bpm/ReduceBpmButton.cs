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

public class ReduceBpmButton : MonoBehaviour, INeedInjection
{
    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private Button button;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private NoteArea noteArea;

    [Inject]
    private ChangeBpmAction changeBpmAction;

    void Start()
    {
        button.OnClickAsObservable().Subscribe(_ => changeBpmAction.ReduceBpmAndNotify(songMeta));
    }
}
