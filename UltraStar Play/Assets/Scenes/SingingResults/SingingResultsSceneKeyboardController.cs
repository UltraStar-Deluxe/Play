using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingingResultsSceneKeyboardController : MonoBehaviour
{

    private const string ContinueToNextSceneShortcut = "return";

    void Update()
    {
        if (Input.GetKeyUp(ContinueToNextSceneShortcut))
        {
            SceneNavigator.Instance.LoadScene(EScene.SongSelectScene);
        }
    }
}
