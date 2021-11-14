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

public class PlayerIndexText : MonoBehaviour, INeedInjection, IInjectionFinishedListener, IExcludeFromSceneInjection
{
    [InjectedInInspector]
    public Text uiText;
    [InjectedInInspector]
    public Image backgroundImage;

    [Inject(Key = "playerProfileIndex")]
    private int playerProfileIndex;

    [Inject(Optional = true)]
    private MicProfile micProfile;

    public void OnInjectionFinished()
    {
        uiText.text = "P" + (playerProfileIndex + 1);
        if (micProfile != null)
        {
            backgroundImage.GetComponent<Image>().color = micProfile.Color;
        }
    }
}
