using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingingResultsSceneKeyboardController : MonoBehaviour
{

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.Escape) || Input.GetKeyUp(KeyCode.Space)
            || Input.GetKeyUp(KeyCode.Backspace) || Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1))
        {
            SingingResultsSceneController.Instance.FinishScene();
        }
    }
}
