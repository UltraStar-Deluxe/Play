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

public class SongEditorMusicVolumeSlider : MonoBehaviour, INeedInjection
{

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private Slider slider;

    [Inject]
    private Settings settings;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = songAudioPlayer.GetComponent<AudioSource>();

        audioSource.volume = settings.SongEditorSettings.MusicVolume;
        slider.value = settings.SongEditorSettings.MusicVolume;
        slider.OnValueChangedAsObservable().Subscribe(newValue =>
        {
            settings.SongEditorSettings.MusicVolume = newValue;
            audioSource.volume = newValue;
        });
    }
}
