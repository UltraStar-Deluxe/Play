using System;
using System.Collections.Generic;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingSceneAudioFadeInControl : INeedInjection, IDisposable
{
    [Inject]
    private GameObject gameObject;

    [Inject]
    private Settings settings;

    private readonly List<int> leanTweenAnimationIds = new();

    public ReactiveProperty<int> FadeInVolumePercent { get; private set; } = new(100);

    public void StartAudioFadeIn(int timeInSeconds)
    {
        if (timeInSeconds <= 0)
        {
            return;
        }

        CancelAudioFadeIn();

        Debug.Log($"Starting audio fade in during {timeInSeconds} seconds");
        FadeInVolumePercent.Value = 0;

        int animationId = LeanTween.value(gameObject, 0, 100, timeInSeconds)
            .setOnUpdate(interpolatedValue => FadeInVolumePercent.Value = (int)interpolatedValue)
            .setOnComplete(() => ResetVolume())
            .id;
        leanTweenAnimationIds.Add(animationId);
    }

    private void ResetVolume()
    {
        FadeInVolumePercent.Value = 100;
    }

    public void CancelAudioFadeIn()
    {
        ResetVolume();
        leanTweenAnimationIds.ForEach(leanTweenAnimationId => LeanTween.cancel(leanTweenAnimationId));
        leanTweenAnimationIds.Clear();
    }

    public void Dispose()
    {
        CancelAudioFadeIn();
    }
}
