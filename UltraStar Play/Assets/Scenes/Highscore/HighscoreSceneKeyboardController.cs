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

public class HighscoreSceneKeyboardController : MonoBehaviour, INeedInjection
{
    [Inject]
    private readonly HighscoreSceneController highscoreSceneController;

    void Update()
    {
        // Go to next scene with Enter, Escape, Mouse button, etc.
        if (Input.GetKeyUp(KeyCode.Escape)
            || Input.GetKeyUp(KeyCode.Return)
            || Input.GetKeyUp(KeyCode.Space)
            || Input.GetMouseButtonDown(0)
            || Input.GetMouseButtonDown(1))
        {
            highscoreSceneController.FinishScene();
        }

        // Change difficulty via arrow keys
        if (Input.GetKeyUp(KeyCode.UpArrow) || Input.GetKeyUp(KeyCode.RightArrow))
        {
            highscoreSceneController.ShowNextDifficulty(1);
        }
        if (Input.GetKeyUp(KeyCode.DownArrow) || Input.GetKeyUp(KeyCode.LeftArrow))
        {
            highscoreSceneController.ShowNextDifficulty(-1);
        }
    }
}
