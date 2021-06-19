using UniInject;
using UnityEngine;
using UniRx;
using PrimeInputActions;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingSceneInputControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private SingSceneController singSceneController;

    [Inject]
    private SongAudioPlayer songAudioPlayer;
    
    private void Start()
    {
        InputManager.GetInputAction(R.InputActions.usplay_skipToNextLyrics).PerformedAsObservable()
            .Subscribe(_ => singSceneController.SkipToNextSingableNote());
        InputManager.GetInputAction(R.InputActions.ui_navigate).PerformedAsObservable()
            .Where(context => context.ReadValue<Vector2>().x > 0)
            .Subscribe(_ => singSceneController.SkipToNextSingableNote());
        
        InputManager.GetInputAction(R.InputActions.usplay_openSongEditor).PerformedAsObservable()
            .Subscribe(_ => singSceneController.OpenSongInEditor());
        
        InputManager.GetInputAction(R.InputActions.usplay_restartSong).PerformedAsObservable()
            .Subscribe(_ => singSceneController.Restart());
        
        InputManager.GetInputAction(R.InputActions.usplay_togglePause).PerformedAsObservable()
            .Subscribe(_ => singSceneController.TogglePlayPause());
        
        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable()
            .Subscribe(_ =>
            {
                if (!songAudioPlayer.IsPlaying)
                {
                    singSceneController.TogglePlayPause();   
                }
                else
                {
                    singSceneController.FinishScene(false);
                }
            });
        
        UltraStarPlayInputManager.AdditionalInputActionInfos.Add(new InputActionInfo("Skip To Next Lyrics", "Navigate Right"));
        UltraStarPlayInputManager.AdditionalInputActionInfos.Add(new InputActionInfo("Toggle Pause", "Double Click"));
    }
}
