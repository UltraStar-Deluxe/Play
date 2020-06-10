using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class GoldenNotesScoreText : CountingNumberText, INeedInjection, IInjectionFinishedListener, IExcludeFromSceneInjection
{
    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private Text text;

    [Inject]
    private PlayerScoreControllerData playerScoreData;

    public void OnInjectionFinished()
    {
        TargetValue = playerScoreData.GoldenNotesTotalScore;
    }
}
