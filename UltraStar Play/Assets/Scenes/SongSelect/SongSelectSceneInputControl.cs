using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;
using UnityEngine.InputSystem;
using PrimeInputActions;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

#pragma warning disable CS0649

public class SongSelectSceneInputControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private SongSelectSceneUiControl songSelectSceneUiControl;
    
    [Inject]
    private SongRouletteControl songRouletteControl;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private FocusableNavigator navigator;

    [Inject]
    public CharacterQuickJumpListControl characterQuickJumpListControl;

    [Inject(UxmlName = R.UxmlNames.inputLegend, Optional = true)]
    private VisualElement inputLegendContainer;

    private readonly ReactiveProperty<string> fuzzySearchText = new ReactiveProperty<string>("");
    public IObservable<string> FuzzySearchText => fuzzySearchText;
    private float fuzzySearchLastInputTimeInSeconds;
    private static readonly float fuzzySearchResetTimeInSeconds = 0.75f;
    
    void Start()
    {
        // Toggle song is favorite
        InputManager.GetInputAction(R.InputActions.usplay_toggleFavorite).PerformedAsObservable()
            .Where(_ => InputManager.GetInputAction(R.InputActions.usplay_toggleFavoritePlaylistActive).InputAction.ReadValue<float>() == 0)
            .Where(_ => InputManager.GetInputAction(R.InputActions.usplay_toggleFavorite).InputAction.ReadValue<float>() >= 1)
            .Subscribe(_ => songSelectSceneUiControl.ToggleSelectedSongIsFavorite());
        
        // Toggle favorite playlist is active
        InputManager.GetInputAction(R.InputActions.usplay_toggleFavoritePlaylistActive).PerformedAsObservable()
            .Where(_ => InputManager.GetInputAction(R.InputActions.usplay_toggleFavoritePlaylistActive).InputAction.ReadValue<float>() >= 1)
            .Subscribe(_ => songSelectSceneUiControl.ToggleFavoritePlaylist());
        
        // Close search or leave scene with Back
        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable()
            .Subscribe(_ => OnBack());
        InputManager.GetInputAction(R.InputActions.usplay_search).PerformedAsObservable()
            .Subscribe(_ => songSelectSceneUiControl.SongSearchControl.FocusSearchTextField());
        
        // Select random song
        InputManager.GetInputAction(R.InputActions.usplay_randomSong).PerformedAsObservable()
            .Where(_ => !songSelectSceneUiControl.IsPlayerSelectOverlayVisible)
            .Subscribe(_ => songSelectSceneUiControl.OnRandomSong());
        
        // Open the song editor
        InputManager.GetInputAction(R.InputActions.usplay_openSongEditor).PerformedAsObservable()
            .Where(_ => !songSelectSceneUiControl.IsMenuOverlayVisible
                        && !songSelectSceneUiControl.IsPlayerSelectOverlayVisible)
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
        
        // Select controls
        InputManager.GetInputAction(R.InputActions.ui_navigate).PerformedAsObservable()
            .Subscribe(context => navigator.OnNavigate(context.ReadValue<Vector2>()));
        InputManager.GetInputAction(R.InputActions.ui_scrollWheel).PerformedAsObservable()
            .Subscribe(OnScrollWheel);
        InputManager.GetInputAction(R.InputActions.usplay_nextSong).PerformedAsObservable()
            .Subscribe(_ => songRouletteControl.SelectNextSong());
        InputManager.GetInputAction(R.InputActions.usplay_previousSong).PerformedAsObservable()
            .Subscribe(_ => songRouletteControl.SelectPreviousSong());

        // Select next / previous character-quick-jump character
        InputManager.GetInputAction(R.InputActions.usplay_nextCharacterQuickJumpCharacter).PerformedAsObservable()
            .Subscribe(_ => characterQuickJumpListControl.SelectNextCharacter());
        InputManager.GetInputAction(R.InputActions.usplay_previousCharacterQuickJumpCharacter).PerformedAsObservable()
            .Subscribe(_ => characterQuickJumpListControl.SelectPreviousCharacter());

        // Toggle song menu overlay
        InputManager.GetInputAction(R.InputActions.usplay_toggleSongMenu).PerformedAsObservable()
            .Where(_ => !songSelectSceneUiControl.IsPlayerSelectOverlayVisible)
            .Where(_ => !songSelectSceneUiControl.IsMenuOverlayVisible)
            .Where(_ => !songSelectSceneUiControl.SongSearchControl.IsSearchTextFieldFocused())
            .Subscribe(_ => songRouletteControl.ToggleSongMenuOverlay());
    }

    private void OnSubmit(InputAction.CallbackContext callbackContext)
    {
        if (songSelectSceneUiControl.SongSearchControl.IsSearchTextFieldFocused())
        {
            // Remove focus
            songSelectSceneUiControl.SubmitSearch();
        }
        else
        {
            navigator.OnSubmit();
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

    private void OnBack()
    {
        if (songSelectSceneUiControl.PlaylistChooserControl.IsPlaylistChooserDropdownOverlayVisible)
        {
            songSelectSceneUiControl.PlaylistChooserControl.HidePlaylistChooserDropdownOverlay();
        }
        else if (songSelectSceneUiControl.SongSearchControl.IsSearchPropertyDropdownVisible)
        {
            songSelectSceneUiControl.SongSearchControl.HideSearchPropertyDropdownOverlay();
        }
        else if (songSelectSceneUiControl.IsPlayerSelectOverlayVisible)
        {
            songSelectSceneUiControl.HidePlayerSelectOverlay();
        }
        else if (songRouletteControl.IsSongMenuOverlayVisible)
        {
            songRouletteControl.HideSongMenuOverlay();
        }
        else if (songSelectSceneUiControl.SongSearchControl.IsSearchTextFieldFocused())
        {
            songSelectSceneUiControl.SubmitSearch();
        }
        else if (songSelectSceneUiControl.IsMenuOverlayVisible)
        {
            songSelectSceneUiControl.HideMenuOverlay();
        }
        else if (songSelectSceneUiControl.IsSongDetailOverlayVisible)
        {
            songSelectSceneUiControl.HideSongDetailOverlay();
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

    private bool IsFuzzySearchActive()
    {
        return !InputUtils.AnyKeyboardModifierPressed()
               && !songSelectSceneUiControl.SongSearchControl.IsSearchTextFieldFocused();
    }
}
