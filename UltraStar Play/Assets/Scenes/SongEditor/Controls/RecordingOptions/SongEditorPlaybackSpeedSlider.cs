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

public class SongEditorPlaybackSpeedSlider : MonoBehaviour, INeedInjection
{

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private Slider slider;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private Settings settings;

    void Start()
    {
        slider.value = settings.SongEditorSettings.MusicPlaybackSpeed;
        songAudioPlayer.PlaybackSpeed = settings.SongEditorSettings.MusicPlaybackSpeed;
        slider.OnValueChangedAsObservable().Subscribe(newValue =>
        {
            float newValueRounded = (float)Math.Round(newValue, 1);
            if (Mathf.Abs(newValueRounded - 1) < 0.1)
            {
                // Round to exactly 1 to eliminate manipulation of playback speed. Otherwise there will be noise in the audio.
                newValueRounded = 1;
            }

            settings.SongEditorSettings.MusicPlaybackSpeed = newValueRounded;
            songAudioPlayer.PlaybackSpeed = newValueRounded;

            if (newValueRounded != newValue)
            {
                slider.value = newValueRounded;
            }
        });
    }
}
