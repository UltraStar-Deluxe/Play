using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.EventSystems;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingingResultsSceneInputControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private SingingResultsSceneController singingResultsSceneController;

    private void Start()
    {
        InputManager.GetInputAction(R.InputActions.usplay_toggleResultGraph).PerformedAsObservable()
            .Subscribe(_ => singingResultsSceneController.ToggleStatistics());

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable()
            .Subscribe(_ => singingResultsSceneController.FinishScene());
        InputManager.GetInputAction(R.InputActions.usplay_start).PerformedAsObservable()
            .Subscribe(_ => singingResultsSceneController.FinishScene());
        InputManager.GetInputAction(R.InputActions.usplay_space).PerformedAsObservable()
            .Subscribe(_ => singingResultsSceneController.FinishScene());
    }
}
