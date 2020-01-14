using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniInject;
using UnityEngine.EventSystems;

#pragma warning disable CS0649

public class SongSelectSceneKeyboardInputController : MonoBehaviour, INeedInjection
{
    private const KeyCode NextSongShortcut = KeyCode.RightArrow;
    private const KeyCode PreviousSongShortcut = KeyCode.LeftArrow;
    private const KeyCode StartSingSceneShortcut = KeyCode.Return;
    private const KeyCode RandomSongShortcut = KeyCode.R;
    private const KeyCode OpenInEditorShortcut = KeyCode.E;

    private const KeyCode QuickSearchSong = KeyCode.LeftControl;
    private const KeyCode QuickSearchArtist = KeyCode.LeftAlt;

    [Inject]
    private EventSystem eventSystem;

    void Update()
    {
        SongSelectSceneController songSelectSceneController = SongSelectSceneController.Instance;
        // Open / close search
        if (Input.GetKeyDown(QuickSearchArtist))
        {
            songSelectSceneController.EnableSearch(SearchInputField.ESearchMode.ByArtist);
        }
        if (Input.GetKeyDown(QuickSearchSong))
        {
            songSelectSceneController.EnableSearch(SearchInputField.ESearchMode.BySongTitle);
        }

        if (songSelectSceneController.IsSearchEnabled())
        {
            // When the search is enabled, then close it via Escape
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                songSelectSceneController.DisableSearch();
            }
        }
        else
        {
            // When the search is not enabled, then open the main menu via Escape or Backspace
            if (Input.GetKeyUp(KeyCode.Escape) || Input.GetKeyUp(KeyCode.Backspace))
            {
                SceneNavigator.Instance.LoadScene(EScene.MainScene);
            }

            if (Input.GetKeyUp(RandomSongShortcut))
            {
                songSelectSceneController.OnRandomSong();
            }
        }

        if (Input.GetKeyUp(NextSongShortcut))
        {
            songSelectSceneController.OnNextSong();
        }

        if (Input.GetKeyUp(PreviousSongShortcut))
        {
            songSelectSceneController.OnPreviousSong();
        }

        if (Input.GetKeyUp(StartSingSceneShortcut)
            || (Input.GetKeyUp(OpenInEditorShortcut) && !songSelectSceneController.IsSearchEnabled()))
        {
            GameObject focusedControl = eventSystem.currentSelectedGameObject;
            bool focusedControlIsSongButton = (focusedControl != null && focusedControl.GetComponent<SongRouletteItem>() != null);
            bool focusedControlIsSearchField = (focusedControl != null && focusedControl.GetComponent<SearchInputField>() != null);
            if (focusedControl == null || focusedControlIsSongButton || focusedControlIsSearchField)
            {
                if (Input.GetKeyUp(StartSingSceneShortcut))
                {
                    songSelectSceneController.StartSingScene();
                }
                else if (Input.GetKeyUp(OpenInEditorShortcut))
                {
                    songSelectSceneController.StartSongEditorScene();
                }
            }
        }
    }
}
