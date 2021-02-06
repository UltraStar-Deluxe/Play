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

public class MainSceneInputControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private SceneNavigator sceneNavigator;
    
	private void Start()
    {
        InputManager.GetInputAction(R.InputActions.usplay_start).PerformedAsObservable()
            .Subscribe(_ => sceneNavigator.LoadScene(EScene.SongSelectScene));
    }
}
