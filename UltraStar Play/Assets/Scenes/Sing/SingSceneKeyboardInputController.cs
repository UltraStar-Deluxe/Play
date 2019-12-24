using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingSceneKeyboardInputController : MonoBehaviour
{
    private const KeyCode SkipShortcut = KeyCode.S;
    private const KeyCode OpenInEditorShortcut = KeyCode.E;
    private const KeyCode RestartShortcut = KeyCode.R;
    private const KeyCode BackToSongSelectShortcut = KeyCode.Backspace;
    private const KeyCode BackToSongSelectShortcut2 = KeyCode.Escape;
    private const KeyCode PauseShortcut = KeyCode.Space;

    void Update()
    {
        if (Input.GetKeyUp(SkipShortcut))
        {
            SingSceneController.Instance.SkipToNextSentence();
        }

        if (Input.GetKeyUp(OpenInEditorShortcut))
        {
            SingSceneController.Instance.OnOpenInEditorClicked();
        }

        if (Input.GetKeyUp(RestartShortcut))
        {
            SingSceneController.Instance.Restart();
        }

        if (Input.GetKeyUp(BackToSongSelectShortcut) || Input.GetKeyUp(BackToSongSelectShortcut2))
        {
            SingSceneController.Instance.FinishScene();
        }

        if (Input.GetKeyUp(PauseShortcut))
        {
            SingSceneController.Instance.TogglePlayPause();
        }
    }
}
