using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UnityEngine.EventSystems;
using System;

public class DefaultButtonSelector : MonoBehaviour
{
    public Button defaultButton;

    void Awake()
    {
        // Try to use itself as default button,
        // if this GameObject has a Button component and no default is set yet.
        if (defaultButton == null)
        {
            defaultButton = GetComponent<Button>();
        }
    }

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
