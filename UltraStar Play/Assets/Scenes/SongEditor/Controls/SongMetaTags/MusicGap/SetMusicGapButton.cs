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

public class SetMusicGapButton : MonoBehaviour, INeedInjection
{

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private Button button;

    [Inject]
    private SetMusicGapAction setMusicGapAction;

    void Start()
    {
        button.OnClickAsObservable().Subscribe(_ => setMusicGapAction.ExecuteAndNotify());
    }
}
