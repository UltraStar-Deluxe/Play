using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingSceneInputControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private SingSceneControl singSceneControl;

    [Inject]
    private SongAudioPlayer songAudioPlayer;
    
    private void Start()
    {
        InputManager.GetInputAction(R.InputActions.usplay_skipToNextLyrics).PerformedAsObservable()
            .Subscribe(_ => singSceneControl.SkipToNextSingableNote());
        InputManager.GetInputAction(R.InputActions.ui_navigate).PerformedAsObservable()
            .Where(context => context.ReadValue<Vector2>().x > 0)
            .Subscribe(_ => singSceneControl.SkipToNextSingableNote());
        
        InputManager.GetInputAction(R.InputActions.usplay_openSongEditor).PerformedAsObservable()
            .Subscribe(_ => singSceneControl.OpenSongInEditor());
        
        InputManager.GetInputAction(R.InputActions.usplay_restartSong).PerformedAsObservable()
            .Subscribe(_ => singSceneControl.Restart());
        
        InputManager.GetInputAction(R.InputActions.usplay_togglePause).PerformedAsObservable()
            .Subscribe(_ => singSceneControl.TogglePlayPause());
        
        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable()
            .Subscribe(_ => OnBack());
    }

    private void OnBack()
    {
        if (!songAudioPlayer.IsPlaying)
        {
            singSceneControl.TogglePlayPause();
        }
        else
        {
            singSceneControl.FinishScene(false);
        }
    }
}
