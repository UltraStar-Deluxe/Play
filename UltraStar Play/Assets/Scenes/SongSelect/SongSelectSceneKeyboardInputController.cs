using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SongSelectSceneKeyboardInputController : MonoBehaviour
{
    private const KeyCode MainMenuShortcut = KeyCode.Escape;
    private const KeyCode MainMenuShortcut2 = KeyCode.Backspace;
    private const KeyCode NextSongShortcut = KeyCode.RightArrow;
    private const KeyCode PreviousSongShortcut = KeyCode.LeftArrow;
    private const KeyCode StartSingSceneShortcut = KeyCode.Return;

    private const KeyCode QuickSearchSong = KeyCode.LeftControl;
    private const KeyCode QuickSearchArtist = KeyCode.LeftAlt;

    void Update()
    {
        SongSelectSceneController songSelectSceneController = SongSelectSceneController.Instance;
        if (Input.GetKeyUp(NextSongShortcut))
        {
            songSelectSceneController.OnNextSong();
        }

        if (Input.GetKeyUp(PreviousSongShortcut))
        {
            songSelectSceneController.OnPreviousSong();
        }

        if (Input.GetKeyUp(MainMenuShortcut) || Input.GetKeyUp(MainMenuShortcut2))
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

        if (Input.GetKeyUp(StartSingSceneShortcut))
        {
            songSelectSceneController.OnStartSingScene();
        }

        if (Input.GetKeyDown(QuickSearchArtist))
        {
            songSelectSceneController.EnableSearch(SearchInputField.ESearchMode.ByArtist);
        }
        if (Input.GetKeyDown(QuickSearchSong))
        {
            songSelectSceneController.EnableSearch(SearchInputField.ESearchMode.BySongTitle);
        }
    }
}
