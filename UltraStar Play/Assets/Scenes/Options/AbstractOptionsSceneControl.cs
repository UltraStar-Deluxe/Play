using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using ProTrans;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public abstract class AbstractOptionsSceneControl : MonoBehaviour, INeedInjection
{
    [Inject]
    protected SceneNavigator sceneNavigator;
    
    [Inject]
    protected TranslationManager translationManager;

    [Inject]
    protected Settings settings;
    
    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    protected Label sceneTitle;
    
    [Inject(UxmlName = R.UxmlNames.backButton)]
    protected Button backButton;

    protected readonly List<IDisposable> disposables = new();
    
	protected virtual void Start() {
        backButton.RegisterCallbackButtonTriggered(() => OnBack());
        backButton.Focus();

        disposables.Add(InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => OnBack()));
	}

    protected virtual void OnBack()
    {
        if (FindObjectOfType<OptionsOverviewSceneControl>() != null)
        {
            // This game object has been loaded additively to the options overview scene.
            // Go back to main menu.
            sceneNavigator.LoadScene(EScene.MainScene);
            return;
        }
        
        sceneNavigator.LoadScene(EScene.OptionsScene);
    }

    private void OnDestroy()
    {
        disposables.ForEach(it => it.Dispose());
        disposables.Clear();
    }
}
