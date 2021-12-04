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

public class SingingResultsSceneContinueButton : MonoBehaviour, INeedInjection
{
    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private Button uiButton;

    [Inject]
    private SingingResultsSceneUiControl SingingResultsSceneUiControl;

    private void Start()
    {
        uiButton.OnClickAsObservable().Subscribe(_ => SingingResultsSceneUiControl.FinishScene());
    }
}
