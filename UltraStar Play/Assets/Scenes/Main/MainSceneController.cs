using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UnityEngine.EventSystems;
using System;

public class MainSceneController : MonoBehaviour
{
    public Button defaultButton;

    void Start()
    {
        if (defaultButton != null)
        {
            defaultButton.Select();
        }
    }

    public void QuitGame()
    {
        ApplicationUtils.QuitOrStopPlayMode();
    }
}
