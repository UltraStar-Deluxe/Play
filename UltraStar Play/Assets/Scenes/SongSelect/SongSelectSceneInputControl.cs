using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniInject;
using UnityEngine.EventSystems;
using UniRx;
using UnityEngine.InputSystem;

#pragma warning disable CS0649

public class SongSelectSceneInputControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private EventSystem eventSystem;

    [Inject]
    private SongSelectSceneController songSelectSceneController;
    
    [Inject]
    private SongSelectSceneControlNavigator songSelectSceneControlNavigator;

    [Inject]
    private SongRouletteController songRouletteController;
    
    private readonly ReactiveProperty<string> fuzzySearchText = new ReactiveProperty<string>("");
    public IObservable<string> FuzzySearchText => fuzzySearchText;
    private float fuzzySearchLastInputTimeInSeconds;
    private static readonly float fuzzySearchResetTimeInSeconds = 0.75f;
    
    void Start()
    {
        // Toggle Search
        InputManager.GetInputAction(R.InputActions.usplay_toggleSearch).PerformedAsObservable()
            .Subscribe(_ => ToggleSearch());
        
        // Toggle song is favorite
        InputManager.GetInputAction(R.InputActions.usplay_toggleFavorite).PerformedAsObservable()
            .Subscribe(_ => songSelectSceneController.ToggleSelectedSongIsFavorite());
        
        // Toggle favorite playlist is active
        InputManager.GetInputAction(R.InputActions.usplay_toggleFavoritePlaylistActive).StartedAsObservable()
            .Subscribe(_ => songSelectSceneController.ToggleFavoritePlaylist());
        
        // Close search or leave scene with Back
        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable()
            .Subscribe(_ => OnBack());
        
        // Select random song
        InputManager.GetInputAction(R.InputActions.usplay_randomSong).PerformedAsObservable()
            .Subscribe(_ => songSelectSceneController.OnRandomSong());
        
        // Open the song editor
        InputManager.GetInputAction(R.InputActions.usplay_openSongEditor).PerformedAsObservable()
            .Subscribe(_ => songSelectSceneController.StartSongEditorScene());
        
        // Toggle selected players
        InputManager.GetInputAction(R.InputActions.usplay_togglePlayers).PerformedAsObservable()
            .Subscribe(_ => songSelectSceneController.ToggleSelectedPlayers());
        
        // Open the sing scene
        InputManager.GetInputAction(R.InputActions.ui_submit).PerformedAsObservable()
            .Subscribe(OnSubmit);
        InputManager.GetInputAction(R.InputActions.usplay_start).PerformedAsObservable()
            .Subscribe(_ => songSelectSceneController.CheckAudioAndStartSingScene());
        
        // Select next / previous song with (hold) arrow keys or mouse wheel
        InputManager.GetInputAction(R.InputActions.ui_navigate).PerformedAsObservable()
            .Subscribe(OnNavigate);
        InputManager.GetInputAction(R.InputActions.ui_scrollWheel).PerformedAsObservable()
            .Subscribe(OnScrollWheel);
    }

    private void OnSubmit(InputAction.CallbackContext callbackContext)
    {
        if (songSelectSceneController.IsSearchEnabled()
            && songSelectSceneController.IsSearchTextInputHasFocus())
        {
            // Close search
            songSelectSceneController.DisableSearch();
        }
        else if (IsNoControlOrSongButtonFocused())
        {
            songSelectSceneController.CheckAudioAndStartSingScene();
        }
        else
        {
            songSelectSceneControlNavigator.SubmitSelectedControl();
        }
    }

    private void OnScrollWheel(InputAction.CallbackContext context)
    {
        if (songSelectSceneController.IsSearchEnabled())
        {
            return;
        }
        
        if (context.ReadValue<Vector2>().y < 0) 
        {
            songRouletteController.SelectNextSong();
        }
        if (context.ReadValue<Vector2>().y > 0)
        {
            songRouletteController.SelectPreviousSong();
        }
    }

    private void OnNavigate(InputAction.CallbackContext context)
    {   
        if (context.ReadValue<Vector2>().x > 0) 
        {
            songRouletteController.SelectNextSong();
        }
        if (context.ReadValue<Vector2>().x < 0)
        {
            songRouletteController.SelectPreviousSong();
        }

        if (context.ReadValue<Vector2>().y > 0)
        {
            songSelectSceneControlNavigator.SelectPreviousControl();
        }
        if (context.ReadValue<Vector2>().y < 0)
        {
            songSelectSceneControlNavigator.SelectNextControl();
        }
    }

    private void OnBack()
    {
        if (songSelectSceneController.IsSearchEnabled())
        {
            songSelectSceneController.DisableSearch();
        }
        else
        {
            SceneNavigator.Instance.LoadScene(EScene.MainScene);
        }
    }

    private void ToggleSearch()
    {
        if (songSelectSceneController.IsSearchEnabled())
        {
            songSelectSceneController.DisableSearch();
        }
        else
        {
            songSelectSceneController.EnableSearch(SearchInputField.ESearchMode.ByTitleOrArtist);
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
            songSelectSceneController.DoFuzzySearch(fuzzySearchText.Value);

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
        GameObject focusedControl = eventSystem.currentSelectedGameObject;
        bool focusedControlIsSongButton = (focusedControl != null && focusedControl.GetComponent<SongRouletteItem>() != null);
        return focusedControl == null || focusedControlIsSongButton;
    }

    private bool IsFuzzySearchActive()
    {
        return !InputUtils.AnyKeyboardModifierPressed()
               && !GameObjectUtils.InputFieldHasFocus(eventSystem);
    }
}
