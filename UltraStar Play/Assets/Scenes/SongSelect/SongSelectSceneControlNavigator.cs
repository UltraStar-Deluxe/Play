using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.EventSystems;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSelectSceneControlNavigator : MonoBehaviour, INeedInjection
{
    [Inject]
    private EventSystem eventSystem;
    
    [Inject]
    private SongSelectSceneUiControl songSelectSceneUiControl;
    
    [Inject]
    private SongSelectPlayerProfileListController playerProfileListController;
    
    [Inject]
    private PlaylistSlider playlistSlider;
    
    [Inject]
    private OrderSlider orderSlider;
    
    [Inject]
    private CharacterQuickJumpBar characterQuickJumpBar;
    
    public SongSelectSceneControls SelectedSceneControl {
        get
        {
            if (eventSystem == null
                || eventSystem.currentSelectedGameObject == null)
            {
                return SongSelectSceneControls.Song;
            }
            
            if (eventSystem.currentSelectedGameObject.GetComponentInParent<SongSelectPlayerProfileListEntry>() != null)
            {
                return SongSelectSceneControls.Player;
            }
            if (eventSystem.currentSelectedGameObject.GetComponentInParent<PlaylistSlider>() != null)
            {
                return SongSelectSceneControls.Playlist;
            }
            if (eventSystem.currentSelectedGameObject.GetComponentInParent<OrderSlider>() != null)
            {
                return SongSelectSceneControls.Order;
            }
            if (eventSystem.currentSelectedGameObject.GetComponentInParent<CharacterQuickJumpBar>() != null)
            {
                return SongSelectSceneControls.CharacterQuickJump;
            }
            if (eventSystem.currentSelectedGameObject.GetComponent<SearchInputField>() != null)
            {
                return SongSelectSceneControls.Search;
            }

            return SongSelectSceneControls.Song;
        }
    }

    private void Start()
    {
        // Navigation in this scene is implemented by this class
        eventSystem.sendNavigationEvents = false;
    }

    public void SelectPreviousControl()
    {
        switch (SelectedSceneControl)
        {
            case SongSelectSceneControls.Song:
                // Nothing to do for deselect songRouletteControl
                SelectSearchControl();
                break;
            case SongSelectSceneControls.Player:
                if (!playerProfileListController.TrySelectPreviousControl())
                {
                    // Nothing to do for deselect playerProfileListControl
                    SelectSongRouletteControl();
                }
                break;
            case SongSelectSceneControls.Playlist:
                DeselectPlaylistSliderControl();
                if (!playerProfileListController.TrySelectPreviousControl())
                {
                    SelectSongRouletteControl();
                }
                break;
            case SongSelectSceneControls.CharacterQuickJump:
                if (!characterQuickJumpBar.TrySelectNextControl())
                {
                    DeselectCharacterQuickJumpBar();
                    SelectPlaylistSliderControl();
                }
                break;
            case SongSelectSceneControls.Order:
                DeselectOrderSliderControl();
                if (!characterQuickJumpBar.TrySelectNextControl())
                {
                    SelectPlaylistSliderControl();
                }
                break;
            case SongSelectSceneControls.Search:
                DeselectSearchControl();
                SelectOrderSliderControl();
                break;
            default:
                // Do nothing
                break;
        }
    }

    public void SelectNextControl()
    {
        switch (SelectedSceneControl)
        {
            case SongSelectSceneControls.Song:
                // Nothing to do for deselect songRouletteControl
                if (!playerProfileListController.TrySelectNextControl())
                {
                    SelectPlaylistSliderControl();
                }
                break;
            case SongSelectSceneControls.Player:
                if (!playerProfileListController.TrySelectNextControl())
                {
                    // Nothing to do for deselect playerProfileListControl
                    SelectPlaylistSliderControl();
                }
                break;
            case SongSelectSceneControls.Playlist:
                if (!characterQuickJumpBar.TrySelectPreviousControl())
                {
                    DeselectPlaylistSliderControl();
                    SelectOrderSliderControl();
                }
                break;
            case SongSelectSceneControls.CharacterQuickJump:
                if (!characterQuickJumpBar.TrySelectPreviousControl())
                {
                    DeselectCharacterQuickJumpBar();
                    SelectOrderSliderControl();
                }
                break;
            case SongSelectSceneControls.Order:
                DeselectOrderSliderControl();
                SelectSearchControl();
                break;
            case SongSelectSceneControls.Search:
                DeselectSearchControl();
                SelectSongRouletteControl();
                break;
            default:
                // Do nothing
                break;
        }
    }

    public void SubmitSelectedControl()
    {
        switch (SelectedSceneControl)
        {
            case SongSelectSceneControls.Song:
                songSelectSceneUiControl.CheckAudioAndStartSingScene();
                break;
            case SongSelectSceneControls.Player:
                if (playerProfileListController.FocusedPlayerProfileControl != null)
                {
                    playerProfileListController.FocusedPlayerProfileControl.SetSelected(
                        !playerProfileListController.FocusedPlayerProfileControl.IsSelected);
                }
                break;
            case SongSelectSceneControls.Playlist:
                playlistSlider.SelectNextItem();
                break;
            case SongSelectSceneControls.Order:
                orderSlider.SelectNextItem();
                break;
            case SongSelectSceneControls.Search:
                songSelectSceneUiControl.UpdateFilteredSongs();
                break;
            case SongSelectSceneControls.CharacterQuickJump:
                // Do nothing. The event is handled by the CharacterQuickJump itself.
                break;
            default:
                // Do nothing.
                break;
        }
    }

    private void SelectSongRouletteControl()
    {
        eventSystem.SetSelectedGameObject(null);
    }
    
    private void DeselectCharacterQuickJumpBar()
    {
        characterQuickJumpBar.GetComponent<RectTransformSlideIntoViewport>().SlideOut();
    }
    
    private void DeselectSearchControl()
    {
        songSelectSceneUiControl.DisableSearch();
    }
    
    private void SelectSearchControl()
    {
        songSelectSceneUiControl.EnableSearch(SearchInputField.ESearchMode.ByTitleOrArtist);
    }

    private void SelectOrderSliderControl()
    {
        orderSlider.GetComponent<RectTransformSlideIntoViewport>().SlideIn();
        orderSlider.nextItemButton.Select();
    }
    
    private void DeselectOrderSliderControl()
    {
        orderSlider.GetComponent<RectTransformSlideIntoViewport>().SlideOut();
    }

    private void SelectPlaylistSliderControl()
    {
        playlistSlider.GetComponent<RectTransformSlideIntoViewport>().SlideIn();
        playlistSlider.nextItemButton.Select();
    }
    
    private void DeselectPlaylistSliderControl()
    {
        playlistSlider.GetComponent<RectTransformSlideIntoViewport>().SlideOut();
    }

}
