using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class HighscoreSceneInputControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private HighscoreSceneControl highscoreSceneControl;
    
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
            .Subscribe(_ => highscoreSceneControl.FinishScene());
        InputManager.GetInputAction(R.InputActions.usplay_start).PerformedAsObservable()
            .Subscribe(_ => highscoreSceneControl.FinishScene());
        InputManager.GetInputAction(R.InputActions.usplay_space).PerformedAsObservable()
            .Subscribe(_ => highscoreSceneControl.FinishScene());
        
        InputManager.GetInputAction(R.InputActions.ui_navigate).PerformedAsObservable()
            .Subscribe(context => OnNavigate(context));
        
        InputManager.GetInputAction(R.InputActions.ui_submit).PerformedAsObservable()
            .Subscribe(_ => highscoreSceneControl.FinishScene());
    }

    private void OnNavigate(InputAction.CallbackContext context)
    {
        Vector2 direction = context.ReadValue<Vector2>();
        if (direction.x > 0)
        {
            highscoreSceneControl.ShowNextDifficulty(1);
        }
        if (direction.x < 0)
        {
            highscoreSceneControl.ShowNextDifficulty(-1);
        }
    }
}
