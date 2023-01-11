using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PartyModeSceneControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private SceneNavigator sceneNavigator;

	private void Start()
    {
        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable()
            .Subscribe(_ => OnBack());
    }

	private void OnBack() {
        sceneNavigator.LoadScene(EScene.MainScene);
	}
}
