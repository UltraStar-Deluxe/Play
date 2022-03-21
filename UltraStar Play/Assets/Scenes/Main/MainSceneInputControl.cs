using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using PrimeInputActions;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MainSceneInputControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private MainSceneControl mainSceneControl;

    [Inject]
    private SceneNavigator sceneNavigator;
    
	private void Start()
    {
        InputManager.GetInputAction(R.InputActions.usplay_start).PerformedAsObservable()
            .Subscribe(_ => sceneNavigator.LoadScene(EScene.SongSelectScene));

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => OnBack());
    }

    private void OnBack()
    {
        if (mainSceneControl.IsNewSongDialogOpen)
        {
            mainSceneControl.CloseNewSongDialog();
        }
        else if (mainSceneControl.IsCloseGameDialogOpen)
        {
            mainSceneControl.CloseQuitGameDialog();
        }
        else
        {
            mainSceneControl.OpenQuitGameDialog();
        }
    }
}
