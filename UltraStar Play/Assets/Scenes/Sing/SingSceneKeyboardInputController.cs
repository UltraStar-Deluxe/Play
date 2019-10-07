using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingSceneKeyboardInputController : MonoBehaviour
{
    private const string SkipShortcut = "s";
    private const string OpenInEditorShortcut = "e";
    private const string RestartShortcut = "r";
    private const string BackToSongSelectShortcut = "backspace";

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

        if (Input.GetKeyUp(BackToSongSelectShortcut))
        {
            SingSceneController.Instance.FinishScene();
        }
    }
}
