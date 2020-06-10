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

public class SingingResultsScenePlayerNameText : MonoBehaviour, INeedInjection, IInjectionFinishedListener, IExcludeFromSceneInjection
{
    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private Text text;

    [Inject]
    private PlayerProfile playerProfile;

    public void OnInjectionFinished()
    {
        text.text = playerProfile.Name;
    }
}
