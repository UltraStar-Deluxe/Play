using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingSceneKeyboardInputController : MonoBehaviour, INeedInjection
{
    private const KeyCode SkipShortcut = KeyCode.S;
    private const KeyCode OpenInEditorShortcut = KeyCode.E;
    private const KeyCode RestartShortcut = KeyCode.R;
    private const KeyCode BackToSongSelectShortcut = KeyCode.Backspace;
    private const KeyCode BackToSongSelectShortcut2 = KeyCode.Escape;
    private const KeyCode PauseShortcut = KeyCode.Space;

    [Inject]
    private SingSceneController singSceneController;

    void Update()
    {
        if (Input.GetKeyUp(SkipShortcut))
        {
            singSceneController.SkipToNextSentence();
        }

        if (Input.GetKeyUp(OpenInEditorShortcut))
        {
            singSceneController.OpenSongInEditor();
        }

        if (Input.GetKeyUp(RestartShortcut))
        {
            singSceneController.Restart();
        }

        if (Input.GetKeyUp(BackToSongSelectShortcut) || Input.GetKeyUp(BackToSongSelectShortcut2))
        {
            singSceneController.FinishScene();
        }

        if (Input.GetKeyUp(PauseShortcut))
        {
            singSceneController.TogglePlayPause();
        }
    }
}
