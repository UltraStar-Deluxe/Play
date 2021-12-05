using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UniRx;
using UnityEngine.InputSystem;
using PrimeInputActions;
using ProTrans;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingingResultsSceneInputControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private SingingResultsSceneUiControl singingResultsSceneUiControl;
    
    [Inject]
    private EventSystem eventSystem;
    
    private void Start()
    {
        // Custom navigation implementation in this scene
        eventSystem.sendNavigationEvents = false;
        
        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable()
            .Subscribe(_ => singingResultsSceneUiControl.FinishScene());
        InputManager.GetInputAction(R.InputActions.usplay_start).PerformedAsObservable()
            .Subscribe(_ => singingResultsSceneUiControl.FinishScene());
        InputManager.GetInputAction(R.InputActions.usplay_space).PerformedAsObservable()
            .Subscribe(_ => singingResultsSceneUiControl.FinishScene());
            
        InputManager.GetInputAction(R.InputActions.ui_submit).PerformedAsObservable()
            .Subscribe(_ => singingResultsSceneUiControl.FinishScene());

    }
}
