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

public class PlayerIndexText : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public Text uiText;
    [InjectedInInspector]
    public Image backgroundImage;

    public void SetBackgroundImageColor(Color color)
    {
        backgroundImage.GetComponent<ImageHueHelper>().SetHueByColor(color);
    }

    public void SetPlayerProfileIndex(int index)
    {
        uiText.text = "P" + (index + 1);
    }
}
