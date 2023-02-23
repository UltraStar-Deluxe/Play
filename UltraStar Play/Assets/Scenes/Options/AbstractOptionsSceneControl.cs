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
    
    protected readonly List<IDisposable> disposables = new();
    
	protected virtual void Start() {
        disposables.Add(InputManager.GetInputAction(R.InputActions.usplay_back)
            .PerformedAsObservable(5)
            .Subscribe(_ => OnBack()));
	}

    protected void OnBack()
    {
        if (TryGoBack())
        {
            InputManager.GetInputAction(R.InputActions.usplay_back).CancelNotifyForThisFrame();
        }
    }

    protected virtual bool TryGoBack()
    {
        return false;
    }
    
    private void OnDestroy()
    {
        disposables.ForEach(it => it.Dispose());
        disposables.Clear();
    }
}
