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

public class ShowNextDifficultyButton : MonoBehaviour, INeedInjection
{
    [Range(-1, 1)]
    public int direction = 1;
    
    [Inject]
    private HighscoreSceneController highscoreSceneController;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private Button uiButton;

    private void Start()
    {
        uiButton.OnClickAsObservable().Subscribe(_ =>
        {
            highscoreSceneController.ShowNextDifficulty(direction);
        });
    }
}
