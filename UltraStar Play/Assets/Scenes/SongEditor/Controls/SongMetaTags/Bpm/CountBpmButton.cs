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

public class CountBpmButton : MonoBehaviour, INeedInjection
{
    private int clickCount;
    private float startTime;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private Button button;

    public Text uiText;

    private void Start()
    {
        button.OnClickAsObservable().Subscribe(_ =>
        {
            StopAllCoroutines();

            if (clickCount == 0)
            {
                startTime = Time.time;
                uiText.text = "First clicks";
            }
            else
            {
                float durationInSeconds = Time.time - startTime;
                if (durationInSeconds > 1)
                {
                    float bpm = 60 * clickCount / durationInSeconds;
                    uiText.text = $"{bpm.ToString("0.00")} BPM";
                }
            }

            // Incrementing the clicks must be done after calculating the duration
            // (on first click, there is no duration yet)
            clickCount++;

            // Automatically reset the counter after a very long delay
            StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(2, ResetCount));
        });
    }

    private void ResetCount()
    {
        clickCount = 0;
    }
}
