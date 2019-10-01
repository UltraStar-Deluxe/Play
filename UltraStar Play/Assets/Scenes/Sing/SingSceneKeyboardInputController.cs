using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingSceneKeyboardInputController : MonoBehaviour
{
    private const string SkipGapShortcut = "s";

    void Update()
    {
        if (Input.GetKeyUp(SkipGapShortcut))
        {
            SingSceneController.Instance.SkipGap();
        }
    }
}
