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
    
    [Inject]
    private SingSceneGovernanceControl singSceneGovernanceControl;
    
    [Inject]
    private Settings settings;
    
    [Inject]
    private FocusableNavigator focusableNavigator;
    
    private void Start()
    {
        InputManager.GetInputAction(R.InputActions.usplay_skipToNextLyrics).PerformedAsObservable()
            .Subscribe(_ => singSceneControl.SkipToNextSingableNoteOrEndOfSong());
        InputManager.GetInputAction(R.InputActions.ui_navigate).PerformedAsObservable()
            .Subscribe(ctx => OnNavigate(ctx.ReadValue<Vector2>()));
        
        InputManager.GetInputAction(R.InputActions.usplay_openSongEditor).PerformedAsObservable()
            .Subscribe(_ => singSceneControl.OpenSongInEditor());
        
        InputManager.GetInputAction(R.InputActions.usplay_restartSong).PerformedAsObservable()
            .Subscribe(_ => singSceneControl.Restart());
        
        InputManager.GetInputAction(R.InputActions.usplay_togglePause).PerformedAsObservable()
            .Subscribe(_ => singSceneControl.TogglePlayPause());
        
        InputManager.GetInputAction(R.InputActions.usplay_singSceneOpenContextMenu).PerformedAsObservable()
            .Subscribe(_ => OnOpenContextMenu());
        
        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable()
            .Subscribe(_ => OnBack());
    }

    private void OnOpenContextMenu()
    {
        singSceneGovernanceControl.OpenContextMenuFromInputAction();
    }

    private void OnNavigate(Vector2 direction)
    {
        if (!ContextMenuPopupControl.OpenContextMenuPopups.IsNullOrEmpty())
        {
            return;
        }
        
        if (direction.x > 0)
        {
            singSceneControl.SkipToNextSingableNoteOrEndOfSong();
        }


        if (direction.y < 0)
        {
            SettingsUtils.DecreaseVolume(settings);
        }
        
        if (direction.y > 0)
        {
            SettingsUtils.IncreaseVolume(settings);
        }
    }

    private void OnBack()
    {
        if (!songAudioPlayer.IsPlaying)
        {
            singSceneControl.TogglePlayPause();
        }
        else
        {
            singSceneControl.FinishScene(false, false);
        }
    }
}
