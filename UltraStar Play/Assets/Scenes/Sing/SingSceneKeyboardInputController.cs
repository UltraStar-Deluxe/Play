using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingSceneKeyboardInputController : MonoBehaviour
{
    private const string SkipShortcut = "s";

    void Update()
    {
        if (Input.GetKeyUp(SkipShortcut))
        {
            SingSceneController.Instance.SkipToNextSentence();
        }
    }
}
