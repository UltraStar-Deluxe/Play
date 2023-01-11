using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingSceneCountdownControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private GameObject gameObject;

    [Inject(UxmlName = R.UxmlNames.countdownLabel)]
    private Label countdownLabel;

    private readonly List<int> leanTweenAnimationIds = new();

    public void OnInjectionFinished()
    {
        if (leanTweenAnimationIds.IsNullOrEmpty())
        {
            countdownLabel.HideByDisplay();
        }
    }

    public void StartCountdown(int timeInSeconds)
    {
        if (timeInSeconds <= 0)
        {
            return;
        }

        CancelCountdown();

        Debug.Log($"Starting counting from {timeInSeconds}");
        countdownLabel.ShowByDisplay();
        for (int i = timeInSeconds; i > 0; i--)
        {
            int animationId = CreateCountdownAnimation(timeInSeconds, i);
            leanTweenAnimationIds.Add(animationId);
        }
    }

    private int CreateCountdownAnimation(int timeInSeconds, int i)
    {
        int animTimeInSeconds = 1;
        LTDescr animation = LeanTween.value(gameObject, 1, 0, animTimeInSeconds)
            .setOnStart(() => countdownLabel.text = i.ToString())
            .setOnUpdate(interpolatedValue =>
            {
                Vector2 scale = new(interpolatedValue, interpolatedValue);
                countdownLabel.style.scale = new StyleScale(scale);
            })
            .setOnComplete(() =>
            {
                if (i <= 1)
                {
                    countdownLabel.HideByDisplay();
                }
            });

        if (i < timeInSeconds)
        {
            animation.setDelay(timeInSeconds - i);
        }

        return animation.id;
    }

    public void CancelCountdown()
    {
        leanTweenAnimationIds.ForEach(leanTweenAnimationId => LeanTween.cancel(leanTweenAnimationId));
        leanTweenAnimationIds.Clear();
    }
}
