using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.EventSystems;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingingResultsSceneKeyboardController : MonoBehaviour, INeedInjection
{
    [Inject]
    private SingingResultsSceneController singingResultsSceneController;

    [Inject]
    private EventSystem eventSystem;

    void Update()
    {
        // Show statistics / graph via S or G or Ctrl
        if (Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.G) || Input.GetKeyUp(KeyCode.LeftControl))
        {
            singingResultsSceneController.ToggleStatistics();
        }

        if (Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.Escape) || Input.GetKeyUp(KeyCode.Space)
            || Input.GetKeyUp(KeyCode.Backspace)
            || ((Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1)) && eventSystem.currentSelectedGameObject == null))
        {
            singingResultsSceneController.FinishScene();
        }
    }
}
