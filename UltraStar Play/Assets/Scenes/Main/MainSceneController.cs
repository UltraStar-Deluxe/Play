using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MainSceneController : MonoBehaviour, INeedInjection
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            QuestionDialog quitDialog = UiManager.Instance.CreateQuestionDialog("Quit?", "Really quit UltraStar Play?");
            quitDialog.yesAction = ApplicationUtils.QuitOrStopPlayMode;
        }
    }
}
