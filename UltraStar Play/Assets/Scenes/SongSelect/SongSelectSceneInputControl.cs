using System;
using System.Collections.Generic;
using System.Linq;
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

    [Inject]
    private FocusableNavigator focusableNavigator;

    [Inject(UxmlName = R.UxmlNames.songListView)]
    private VisualElement songListView;

    [Inject(UxmlName = R.UxmlNames.inputLegend, Optional = true)]
    private VisualElement inputLegendContainer;

    [Inject(UxmlName = R.UxmlNames.searchTextField)]
    private TextField searchTextField;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private PanelHelper panelHelper;

    private readonly ReactiveProperty<string> fuzzySearchText = new("");
    public IObservable<string> FuzzySearchText => fuzzySearchText;
    private float fuzzySearchLastInputTimeInSeconds;
    private static readonly float fuzzySearchResetTimeInSeconds = 0.75f;

    void Start()
    {
        focusableNavigator.NoNavigationTargetFoundInListViewCallback = OnNoNavigationTargetFoundInListView;
        focusableNavigator.BeforeNavigationInListViewCallback = OnBeforeNavigationInListView;

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

        // Toggle song menu
        InputManager.GetInputAction(R.InputActions.usplay_toggleSongMenu).PerformedAsObservable()
            .Where(_ => !IsTextFieldFocused() && fuzzySearchText.Value.IsNullOrEmpty())
            .Subscribe(_ => songRouletteControl.OpenSelectedEntryContextMenu());

        // Open the sing scene
        InputManager.GetInputAction(R.InputActions.usplay_start).PerformedAsObservable()
            .Subscribe(_ => songSelectSceneControl.AttemptStartSelectedEntry());

        // Select controls
        InputManager.GetInputAction(R.InputActions.ui_scrollWheel).PerformedAsObservable()
            .Subscribe(OnScrollWheel);
        InputManager.GetInputAction(R.InputActions.usplay_nextSong).PerformedAsObservable()
            .Subscribe(_ => songRouletteControl.SelectNextEntry());
        InputManager.GetInputAction(R.InputActions.usplay_previousSong).PerformedAsObservable()
            .Subscribe(_ => songRouletteControl.SelectPreviousEntry());

        // Navigate to parent folder
        InputManager.GetInputAction(R.InputActions.usplay_navigateToParentFolder).PerformedAsObservable()
            .Where(_ => !IsTextFieldFocused() && fuzzySearchText.Value.IsNullOrEmpty())
            .Subscribe(ctx => songSelectSceneControl.TryNavigateToParentFolder());

        // Navigate to parent folder with right click on song list
        songListView.RegisterCallback<PointerUpEvent>(evt =>
        {
            VisualElement targetElement = evt.target as VisualElement;
            if (targetElement == null)
            {
                return;
            }
            List<VisualElement> ancestors = targetElement.GetAncestors();
            if (ancestors.AnyMatch(ancestor => ancestor.name == R.UxmlNames.songEntryUiRoot))
            {
                return;
            }

            if (evt.button == 1)
            {
                songSelectSceneControl.TryNavigateToParentFolder();
            }
        });
    }

    private bool IsTextFieldFocused()
    {
        return uiDocument.rootVisualElement.focusController.focusedElement is TextField;
    }

    private bool IsSearchTextFieldFocused()
    {
        return searchTextField.focusController.focusedElement == searchTextField
               || !searchTextField.value.IsNullOrEmpty();
    }

    private bool OnBeforeNavigationInListView(NavigationParameters navigationParameters)
    {
        if (navigationParameters.focusedVisualElement != songListView
            || songRouletteControl.Selection.Value.Entry == null)
        {
            return false;
        }

        if (navigationParameters.navigationDirection.x < 0
            && songRouletteControl.Selection.Value.Index == 0)
        {
            // Wrap around: select last song
            songRouletteControl.SelectVeryLastEntry();
            return true;
        }

        if (navigationParameters.navigationDirection.x > 0
            && songRouletteControl.Selection.Value.Index == songRouletteControl.Entries.Count - 1)
        {
            // Wrap around: select last song
            songRouletteControl.SelectVeryFirstEntry();
            return true;
        }

        return false;
    }

    private bool OnNoNavigationTargetFoundInListView(NoNavigationTargetFoundEvent evt)
    {
        if (evt.FocusedVisualElement == songListView
            && evt.NavigationDirection.x > 0
            && songRouletteControl.Entries.Count > 2)
        {
            // Wrap selection, i.e. select first song
            songRouletteControl.SelectEntryByIndex(0);
            return true;
        }
        else if (evt.FocusedVisualElement == songListView
            && evt.NavigationDirection.x < 0
            && songRouletteControl.Entries.Count > 2)
        {
            // Wrap selection, i.e. select first song
            songRouletteControl.SelectEntryByIndex(songRouletteControl.Entries.Count - 1);
            return true;
        }
        return false;
    }

    private void OnScrollWheel(InputAction.CallbackContext context)
    {
        if (!IsPointerOverSongList())
        {
            return;
        }

        if (context.ReadValue<Vector2>().y < 0)
        {
            songRouletteControl.SelectNextEntry();
        }
        if (context.ReadValue<Vector2>().y > 0)
        {
            songRouletteControl.SelectPreviousEntry();
        }
    }

    private bool IsPointerOverSongList()
    {
        VisualElement elementUnderPointer = VisualElementUtils.GetElementUnderPointer(uiDocument, panelHelper);
        if (elementUnderPointer == null)
        {
            return false;
        }
        return elementUnderPointer == songListView
               || elementUnderPointer.GetFirstAncestorOfType<ListView>() != null
               || elementUnderPointer.GetFirstAncestorOfType<ListViewH>() != null;
    }

    private void OnBack()
    {
        if (songSearchControl.IsSearchPropertyDropdownVisible)
        {
            songSearchControl.HideSearchPropertyDropdownOverlay();
        }
        else if (IsSearchTextFieldFocused())
        {
            songSelectSceneControl.OnCancelSearch();
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
        InputSystem.devices
            .OfType<Keyboard>()
            .ForEach(keyboard => keyboard.onTextInput += OnKeyboardTextInput);
    }

    private void OnDisable()
    {
        InputSystem.devices
            .OfType<Keyboard>()
            .ForEach(keyboard => keyboard.onTextInput -= OnKeyboardTextInput);
    }

    private void OnKeyboardTextInput(char newChar)
    {
        if (newChar == (int)KeyCode.Escape
            || newChar == (int)KeyCode.Return
            || (newChar == (int)KeyCode.Backspace && fuzzySearchText.Value.IsNullOrEmpty())
            || (newChar == (int)KeyCode.Space && fuzzySearchText.Value.IsNullOrEmpty()))
        {
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
               && !IsSearchTextFieldFocused()
               && !IsTextFieldFocused();
    }

    private void OnDestroy()
    {
        focusableNavigator.NoNavigationTargetFoundInListViewCallback = null;
        focusableNavigator.BeforeNavigationInListViewCallback = null;
    }
}
