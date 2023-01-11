using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingSceneAudioFadeInControl : INeedInjection, IDisposable, IInjectionFinishedListener
{
    [Inject]
    private GameObject gameObject;

    [Inject]
    private Settings settings;

    private readonly List<int> leanTweenAnimationIds = new();

    private int originalVolume;

    public void OnInjectionFinished()
    {
        originalVolume = settings.AudioSettings.VolumePercent;
    }

    public void StartAudioFadeIn(int timeInSeconds)
    {
        if (timeInSeconds <= 0)
        {
            return;
        }

        CancelAudioFadeIn();

        Debug.Log($"Starting audio fade in during {timeInSeconds} seconds");
        settings.AudioSettings.VolumePercent = 0;
        AudioListener.volume = 0;

        int animationId = LeanTween.value(gameObject, 0, 1, timeInSeconds)
            .setOnUpdate(interpolatedValue => settings.AudioSettings.VolumePercent = (int)(originalVolume * interpolatedValue))
            .setOnComplete(() => ResetVolume())
            .id;
        leanTweenAnimationIds.Add(animationId);
    }

    private void ResetVolume()
    {
        settings.AudioSettings.VolumePercent = originalVolume;
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
