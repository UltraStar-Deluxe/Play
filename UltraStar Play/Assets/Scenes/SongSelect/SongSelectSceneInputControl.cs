using System;
using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

#pragma warning disable CS0649

public class SongSelectSceneInputControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private SongSelectSceneControl songSelectSceneControl;
    
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

    private readonly ReactiveProperty<string> fuzzySearchText = new("");
    public IObservable<string> FuzzySearchText => fuzzySearchText;
    private float fuzzySearchLastInputTimeInSeconds;
    private static readonly float fuzzySearchResetTimeInSeconds = 0.75f;
    
    void Start()
    {
        // Toggle song is favorite
        InputManager.GetInputAction(R.InputActions.usplay_toggleFavorite).PerformedAsObservable()
            .Where(_ => InputManager.GetInputAction(R.InputActions.usplay_toggleFavoritePlaylistActive).InputAction.ReadValue<float>() == 0)
            .Where(_ => InputManager.GetInputAction(R.InputActions.usplay_toggleFavorite).InputAction.ReadValue<float>() >= 1)
            .Subscribe(_ => songSelectSceneControl.ToggleSelectedSongIsFavorite());
        
        // Toggle favorite playlist is active
        InputManager.GetInputAction(R.InputActions.usplay_toggleFavoritePlaylistActive).PerformedAsObservable()
            .Where(_ => InputManager.GetInputAction(R.InputActions.usplay_toggleFavoritePlaylistActive).InputAction.ReadValue<float>() >= 1)
            .Subscribe(_ => songSelectSceneControl.ToggleFavoritePlaylist());
        
        // Close search or leave scene with Back
        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable()
            .Subscribe(_ => OnBack());
        InputManager.GetInputAction(R.InputActions.usplay_search).PerformedAsObservable()
            .Subscribe(_ => songSelectSceneControl.SongSearchControl.FocusSearchTextField());
        
        // Select random song
        InputManager.GetInputAction(R.InputActions.usplay_randomSong).PerformedAsObservable()
            .Subscribe(_ => songSelectSceneControl.OnRandomSong());
        
        // Open the song editor
        InputManager.GetInputAction(R.InputActions.usplay_openSongEditor).PerformedAsObservable()
            .Subscribe(_ => songSelectSceneControl.StartSongEditorScene());
        
        // Toggle selected players
        InputManager.GetInputAction(R.InputActions.usplay_togglePlayers).PerformedAsObservable()
            .Subscribe(_ => songSelectSceneControl.ToggleSelectedPlayers());
        
        // Open the sing scene
        InputManager.GetInputAction(R.InputActions.ui_submit).PerformedAsObservable()
            .Subscribe(OnSubmit);
        InputManager.GetInputAction(R.InputActions.usplay_start).PerformedAsObservable()
            .Subscribe(_ => songSelectSceneControl.CheckAudioAndStartSingScene());
        
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
            .Where(_ => !songSelectSceneControl.SongSearchControl.IsSearchTextFieldFocused())
            .Subscribe(_ => songRouletteControl.ToggleSongMenuOverlay());
    }

    private void OnSubmit(InputAction.CallbackContext callbackContext)
    {
        if (songSelectSceneControl.SongSearchControl.IsSearchTextFieldFocused())
        {
            // Remove focus
            songSelectSceneControl.SubmitSearch();
        }
        else
        {
            navigator.OnSubmit();
        }
    }

    private void OnScrollWheel(InputAction.CallbackContext context)
    {
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
        if (songSelectSceneControl.PlaylistChooserControl.IsPlaylistChooserDropdownOverlayVisible)
        {
            songSelectSceneControl.PlaylistChooserControl.HidePlaylistChooserDropdownOverlay();
        }
        else if (songSelectSceneControl.IsSearchExpressionInfoOverlayVisible)
        {
            songSelectSceneControl.CloseSearchExpressionHelp();
        }
        else if (songSelectSceneControl.SongSearchControl.IsSearchPropertyDropdownVisible)
        {
            songSelectSceneControl.SongSearchControl.HideSearchPropertyDropdownOverlay();
        }
        else if (songRouletteControl.IsSongMenuOverlayVisible)
        {
            songRouletteControl.HideSongMenuOverlay();
        }
        else if (songSelectSceneControl.SongSearchControl.IsSearchTextFieldFocused())
        {
            songSelectSceneControl.SubmitSearch();
        }
        else if (songSelectSceneControl.IsPlaylistActive())
        {
            songSelectSceneControl.ResetPlaylistSelection();
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
            songSelectSceneControl.DoFuzzySearch(fuzzySearchText.Value);

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
               && !songSelectSceneControl.SongSearchControl.IsSearchTextFieldFocused();
    }
}
