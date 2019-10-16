using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SongSelectSceneKeyboardInputController : MonoBehaviour
{
    private const KeyCode NextSongShortcut = KeyCode.RightArrow;
    private const KeyCode PreviousSongShortcut = KeyCode.LeftArrow;
    private const KeyCode BackToMainMenuShortcut = KeyCode.Escape;
    private const KeyCode StartSingSceneShortcut = KeyCode.Return;

    void Update()
    {
        if (Input.GetKeyUp(NextSongShortcut))
        {
            SongSelectSceneController.Instance.OnNextSong();
        }

        if (Input.GetKeyUp(PreviousSongShortcut))
        {
            SongSelectSceneController.Instance.OnPreviousSong();
        }

        if (Input.GetKeyUp(BackToMainMenuShortcut))
        {
            SceneNavigator.Instance.LoadScene(EScene.MainScene);
        }

        if (Input.GetKeyUp(StartSingSceneShortcut))
        {
            SongSelectSceneController.Instance.OnStartSingScene();
        }
    }
}
