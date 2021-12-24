using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniInject;
using UniRx;
using UnityEngine.InputSystem;
using PrimeInputActions;
using UnityEngine.UIElements;

#pragma warning disable CS0649

public class SongSelectSceneInputControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private SongSelectSceneUiControl songSelectSceneUiControl;
    
    // [Inject]
    // private SongSelectSceneControlNavigator songSelectSceneControlNavigator;

    [Inject]
    private SongRouletteControl songRouletteControl;

    [Inject]
    private SceneNavigator sceneNavigator;

    private readonly ReactiveProperty<string> fuzzySearchText = new ReactiveProperty<string>("");
    public IObservable<string> FuzzySearchText => fuzzySearchText;
    private float fuzzySearchLastInputTimeInSeconds;
    private static readonly float fuzzySearchResetTimeInSeconds = 0.75f;
    
    void Start()
    {
        // Toggle song is favorite
        InputManager.GetInputAction(R.InputActions.usplay_toggleFavorite).PerformedAsObservable()
            .Subscribe(_ => songSelectSceneUiControl.ToggleSelectedSongIsFavorite());
        
        // Toggle favorite playlist is active
        InputManager.GetInputAction(R.InputActions.usplay_toggleFavoritePlaylistActive).StartedAsObservable()
            .Subscribe(_ => songSelectSceneUiControl.ToggleFavoritePlaylist());
        
        // Close search or leave scene with Back
        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable()
            .Subscribe(_ => OnBack());
        
        // Select random song
        InputManager.GetInputAction(R.InputActions.usplay_randomSong).PerformedAsObservable()
            .Where(_ => !songSelectSceneUiControl.IsPlayerSelectOverlayVisible)
            .Subscribe(_ => songSelectSceneUiControl.OnRandomSong());
        
        // Open the song editor
        InputManager.GetInputAction(R.InputActions.usplay_openSongEditor).PerformedAsObservable()
            .Subscribe(_ => songSelectSceneUiControl.StartSongEditorScene());
        
        // Toggle selected players
        InputManager.GetInputAction(R.InputActions.usplay_togglePlayers).PerformedAsObservable()
            .Where(_ => songSelectSceneUiControl.IsPlayerSelectOverlayVisible)
            .Subscribe(_ => songSelectSceneUiControl.ToggleSelectedPlayers());
        
        // Open the sing scene
        InputManager.GetInputAction(R.InputActions.ui_submit).PerformedAsObservable()
            .Subscribe(OnSubmit);
        InputManager.GetInputAction(R.InputActions.usplay_start).PerformedAsObservable()
            .Subscribe(_ => songSelectSceneUiControl.CheckAudioAndStartSingScene());
        
        // Select next / previous song with (hold) arrow keys or mouse wheel
        InputManager.GetInputAction(R.InputActions.ui_navigate).PerformedAsObservable()
            .Subscribe(OnNavigate);
        InputManager.GetInputAction(R.InputActions.ui_scrollWheel).PerformedAsObservable()
            .Subscribe(OnScrollWheel);

        // Toggle song menu overlay
        InputManager.GetInputAction(R.InputActions.usplay_space).PerformedAsObservable()
            .Subscribe(_ => songRouletteControl.ToggleSongMenuOverlay());
    }

    private void OnSubmit(InputAction.CallbackContext callbackContext)
    {
        if (songSelectSceneUiControl.IsPlayerSelectOverlayVisible
            || songRouletteControl.IsSongMenuOverlayVisible)
        {
            return;
        }

        if (songSelectSceneUiControl.IsSearchTextFieldFocused())
        {
            // Remove focus
            songSelectSceneUiControl.SubmitSearch();
        }
        else if (IsNoControlOrSongButtonFocused())
        {
            songSelectSceneUiControl.CheckAudioAndStartSingScene();
        }
        else
        {
            // songSelectSceneControlNavigator.SubmitSelectedControl();
        }
    }

    private void OnScrollWheel(InputAction.CallbackContext context)
    {
        if (songSelectSceneUiControl.IsPlayerSelectOverlayVisible)
        {
            return;
        }

        if (context.ReadValue<Vector2>().y < 0) 
        {
            songRouletteControl.SelectNextSong();
        }
        if (context.ReadValue<Vector2>().y > 0)
        {
            songRouletteControl.SelectPreviousSong();
        }
    }

    private void OnNavigate(InputAction.CallbackContext context)
    {
        if (songSelectSceneUiControl.IsPlayerSelectOverlayVisible
            || songSelectSceneUiControl.IsSearchTextFieldFocused())
        {
            return;
        }

        if (context.ReadValue<Vector2>().x > 0) 
        {
            songRouletteControl.SelectNextSong();
        }
        if (context.ReadValue<Vector2>().x < 0)
        {
            songRouletteControl.SelectPreviousSong();
        }

        // if (context.ReadValue<Vector2>().y > 0)
        // {
        //     songSelectSceneControlNavigator.SelectPreviousControl();
        // }
        // if (context.ReadValue<Vector2>().y < 0)
        // {
        //     songSelectSceneControlNavigator.SelectNextControl();
        // }
    }

    private void OnBack()
    {
        if (songSelectSceneUiControl.IsPlayerSelectOverlayVisible)
        {
            songSelectSceneUiControl.HidePlayerSelectOverlay();
        }
        else if (songRouletteControl.IsSongMenuOverlayVisible)
        {
            songRouletteControl.HideSongMenuOverlay();
        }
        else if (songSelectSceneUiControl.IsSearchTextFieldFocused())
        {
            songSelectSceneUiControl.SubmitSearch();
        }
        else if (songSelectSceneUiControl.IsPlaylistActive())
        {
            songSelectSceneUiControl.ResetPlaylistSelection();
        }
        else
        {
            sceneNavigator.LoadScene(EScene.MainScene);
        }
    }

    private void OnEnable()
    {
        Keyboard keyboard = InputSystem.GetDevice<Keyboard>();
        if (keyboard != null)
        {
            keyboard.onTextInput += OnKeyboardTextInput;
        }
    }

    private void OnDisable()
    {
        Keyboard keyboard = InputSystem.GetDevice<Keyboard>();
        if (keyboard != null)
        {
            keyboard.onTextInput -= OnKeyboardTextInput;
        }
    }

    private void OnKeyboardTextInput(char newChar)
    {
        if (newChar == 27)
        {
            // Ignore ESCAPE key
            return;
        }

        if (IsFuzzySearchActive())
        {
            // When there was no keyboard input for a while, then reset the search term.
            CheckResetFuzzySearchText();
            
            fuzzySearchLastInputTimeInSeconds = Time.time;
            if (newChar == '\b' && fuzzySearchText.Value.Length > 0)
            {
                // Backspace. Remove last character.
                fuzzySearchText.Value = fuzzySearchText.Value.Substring(0, fuzzySearchText.Value.Length - 1);
            }
            else
            {
                fuzzySearchText.Value += newChar;
            }
            songSelectSceneUiControl.DoFuzzySearch(fuzzySearchText.Value);

            StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(fuzzySearchResetTimeInSeconds, () => CheckResetFuzzySearchText()));
        }
    }

    private void CheckResetFuzzySearchText()
    {
        if (fuzzySearchLastInputTimeInSeconds + fuzzySearchResetTimeInSeconds < Time.time)
        {
            fuzzySearchText.Value = "";
        }
    }

    // Check that no other control has the focus, such as a checkbox of a player profile.
    // In such a case, keyboard input could be intended to change the controls state, e.g., (de)select the checkbox.
    private bool IsNoControlOrSongButtonFocused()
    {
        // TODO: Migrate to UIToolkit
        return true;
    }

    private bool IsFuzzySearchActive()
    {
        return !InputUtils.AnyKeyboardModifierPressed()
               && !songSelectSceneUiControl.IsSearchTextFieldFocused();
    }
}
