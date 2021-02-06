using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.InputSystem;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class HighscoreSceneInputControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private HighscoreSceneController highscoreSceneController;

    private void Start()
    {
        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable()
            .Subscribe(_ => highscoreSceneController.FinishScene());
        InputManager.GetInputAction(R.InputActions.usplay_start).PerformedAsObservable()
            .Subscribe(_ => highscoreSceneController.FinishScene());
        InputManager.GetInputAction(R.InputActions.usplay_space).PerformedAsObservable()
            .Subscribe(_ => highscoreSceneController.FinishScene());
        
        InputManager.GetInputAction(R.InputActions.ui_navigate).PerformedAsObservable()
            .Subscribe(context => OnNavigate(context));
    }

    private void OnNavigate(InputAction.CallbackContext context)
    {
        Vector2 direction = context.ReadValue<Vector2>();
        if (direction.x > 0)
        {
            highscoreSceneController.ShowNextDifficulty(1);
        }
        if (direction.x < 0)
        {
            highscoreSceneController.ShowNextDifficulty(-1);
        }
    }
}
