using System;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingSceneCountdownControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private GameObject gameObject;

    [Inject(UxmlName = R.UxmlNames.countdownLabel)]
    private Label countdownLabel;

    private float passedTimeInSeconds = -1;
    private float targetTimeInSeconds = -1;

    public void OnInjectionFinished()
    {
        countdownLabel.HideByDisplay();
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
        passedTimeInSeconds = 0;
        targetTimeInSeconds = timeInSeconds;
    }

    public void Update(float deltaTimeInSeconds)
    {
        if (passedTimeInSeconds < 0
            || targetTimeInSeconds < 0)
        {
            return;
        }

        passedTimeInSeconds += deltaTimeInSeconds;
        if (passedTimeInSeconds >= targetTimeInSeconds)
        {
            // Countdown done
            CancelCountdown();
            return;
        }

        countdownLabel.ShowByDisplay();
        float passedTimeInWholeSeconds = (float)Math.Truncate(passedTimeInSeconds);
        float passedTimeInSingleSecond = (float)(passedTimeInSeconds - passedTimeInWholeSeconds);
        int missingSeconds = (int)Math.Ceiling(targetTimeInSeconds - passedTimeInSeconds);
        countdownLabel.SetTranslatedText(Translation.Of(missingSeconds.ToString()));

        Vector2 scale = new(1 - passedTimeInSingleSecond, 1 - passedTimeInSingleSecond);
        countdownLabel.style.scale = new StyleScale(scale);
    }

    public void CancelCountdown()
    {
        passedTimeInSeconds = -1;
        targetTimeInSeconds = -1;
        countdownLabel.HideByDisplay();
    }
}
