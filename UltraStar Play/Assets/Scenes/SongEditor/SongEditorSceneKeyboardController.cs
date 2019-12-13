using System.Collections;
using System.Collections.Generic;
using UniInject;
using UnityEngine;

#pragma warning disable CS0649

public class SongEditorSceneKeyboardController : MonoBehaviour, INeedInjection
{

    [Inject(searchMethod = SearchMethods.FindObjectOfType)]
    private SongEditorSceneController songEditorSceneController;

    public void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            songEditorSceneController.TogglePlayPause();
        }
    }

}
