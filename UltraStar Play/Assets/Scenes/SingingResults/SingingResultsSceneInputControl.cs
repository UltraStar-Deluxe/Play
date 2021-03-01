using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UniRx;
using UnityEngine.InputSystem;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingingResultsSceneInputControl : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public Button continueButton;
    
    [InjectedInInspector]
    public Button toggleStatisticsButton;
    
    [Inject]
    private SingingResultsSceneController singingResultsSceneController;
    
    [Inject]
    private EventSystem eventSystem;
    
    private void Start()
    {
        // Custom navigation implementation in this scene
        eventSystem.sendNavigationEvents = false;
        
        InputManager.GetInputAction(R.InputActions.usplay_toggleResultGraph).PerformedAsObservable()
            .Subscribe(_ => singingResultsSceneController.ToggleStatistics());

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable()
            .Subscribe(_ => singingResultsSceneController.FinishScene());
        InputManager.GetInputAction(R.InputActions.usplay_start).PerformedAsObservable()
            .Subscribe(_ => singingResultsSceneController.FinishScene());
        InputManager.GetInputAction(R.InputActions.usplay_space).PerformedAsObservable()
            .Subscribe(_ => singingResultsSceneController.FinishScene());
            
        InputManager.GetInputAction(R.InputActions.ui_navigate).PerformedAsObservable()
            .Subscribe(_  => OnNavigate());
        InputManager.GetInputAction(R.InputActions.ui_submit).PerformedAsObservable()
            .Subscribe(_ => OnSubmit());

    }

    private void OnSubmit()
    {
        if (eventSystem.currentSelectedGameObject == null)
        {
            return;
        }
        Button selectedButton = eventSystem.currentSelectedGameObject.GetComponent<Button>();
        if (selectedButton == null)
        {
            return;
        }
        selectedButton.OnSubmit(new BaseEventData(eventSystem));
    }

    private void OnNavigate()
    {
        // Toggle between buttons
        if (eventSystem.currentSelectedGameObject == toggleStatisticsButton.gameObject)
        {
            continueButton.Select();
            return;
        }
        toggleStatisticsButton.Select();
    }
}
