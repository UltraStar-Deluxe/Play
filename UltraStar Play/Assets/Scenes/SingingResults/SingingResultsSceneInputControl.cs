using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingingResultsSceneInputControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private SingingResultsSceneControl singingResultsSceneControl;
    
    [Inject(Optional = true)]
    private EventSystem eventSystem;
    
    private void Start()
    {
        // Custom navigation implementation in this scene
        if (eventSystem != null)
        {
            eventSystem.sendNavigationEvents = false;
        }
        
        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable()
            .Subscribe(_ => singingResultsSceneControl.Continue());
        InputManager.GetInputAction(R.InputActions.usplay_start).PerformedAsObservable()
            .Subscribe(_ => singingResultsSceneControl.Continue());
        InputManager.GetInputAction(R.InputActions.usplay_space).PerformedAsObservable()
            .Subscribe(_ => singingResultsSceneControl.Continue());
    }
}
