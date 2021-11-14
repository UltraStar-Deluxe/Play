using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using ProTrans;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class VolumeControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private Settings settings;

    private void Start()
    {
        UpdateVolumeInScene();
        settings.AudioSettings
            .ObserveEveryValueChanged(audioSettings => audioSettings.VolumePercent)
            .Subscribe(newValue => UpdateVolumeInScene());
    }

    private void UpdateVolumeInScene()
    {
        AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
        if (!audioSources.IsNullOrEmpty())
        {
            audioSources.ForEach(audioSource => audioSource.volume = settings.AudioSettings.VolumePercent / 100.0f);
        }

        AudioListener.volume = settings.AudioSettings.VolumePercent / 100.0f;
    }
}
