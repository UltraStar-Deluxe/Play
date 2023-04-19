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
    private SongSearchControl songSearchControl;
    
    [Inject]
    private SongRouletteControl songRouletteControl;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject(UxmlName = R.UxmlNames.songListView)]
    private VisualElement songListView;
    
    [Inject(UxmlName = R.UxmlNames.inputLegend, Optional = true)]
    private VisualElement inputLegendContainer;

    private readonly ReactiveProperty<string> fuzzySearchText = new("");
    public IObservable<string> FuzzySearchText => fuzzySearchText;
    private float fuzzySearchLastInputTimeInSeconds;
    private static readonly float fuzzySearchResetTimeInSeconds = 0.75f;

    private bool isPointerOverSongList;
    
    void Start()
    {
        songListView.RegisterCallback<PointerEnterEvent>(_ => isPointerOverSongList = true, TrickleDown.TrickleDown);
        songListView.RegisterCallback<PointerLeaveEvent>(_ => isPointerOverSongList = false, TrickleDown.TrickleDown);
        songListView.ReleaseMouse();
            
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
            .Subscribe(_ => songSearchControl.FocusSearchTextField());
        
        // Select random song
        InputManager.GetInputAction(R.InputActions.usplay_randomSong).PerformedAsObservable()
            .Subscribe(_ => songSelectSceneControl.SelectRandomSong());
        
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
            .Subscribe(_ => songSelectSceneControl.AttemptStartSong());
        
        // Select controls
        InputManager.GetInputAction(R.InputActions.ui_scrollWheel).PerformedAsObservable()
            .Subscribe(OnScrollWheel);
        InputManager.GetInputAction(R.InputActions.usplay_nextSong).PerformedAsObservable()
            .Subscribe(_ => songRouletteControl.SelectNextSong());
        InputManager.GetInputAction(R.InputActions.usplay_previousSong).PerformedAsObservable()
            .Subscribe(_ => songRouletteControl.SelectPreviousSong());
    }

    private void OnSubmit(InputAction.CallbackContext callbackContext)
    {
        if (songSearchControl.IsSearchTextFieldFocused())
        {
            songSelectSceneControl.SubmitSearch();
        }
        else if (songListView.focusController.focusedElement == songListView)
        {
            songSelectSceneControl.AttemptStartSong();
        }
    }

    private void OnScrollWheel(InputAction.CallbackContext context)
    {
        if (!isPointerOverSongList)
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
        if (songSearchControl.IsSearchPropertyDropdownVisible)
        {
            songSearchControl.HideSearchPropertyDropdownOverlay();
        }
        else if (songSearchControl.IsSearchTextFieldFocused())
        {
            songSelectSceneControl.SubmitSearch();
        }
        else if (songSelectSceneControl.SongQueueSlideInControl.Visible.Value)
        {
            songSelectSceneControl.SongQueueSlideInControl.SlideOut();
        }
        else if (songSelectSceneControl.ModifiersOverlaySlideInControl.Visible.Value)
        {
            songSelectSceneControl.ModifiersOverlaySlideInControl.SlideOut();
        }
        else
        {
            songSelectSceneControl.QuitSongSelect();
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
               && !songSearchControl.IsSearchTextFieldFocused();
    }
}
