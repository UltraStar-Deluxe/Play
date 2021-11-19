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

public class SongEditorMidiVelocitySlider : MonoBehaviour, INeedInjection
{
    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private Slider slider;

    [Inject]
    private Settings settings;

    void Start()
    {
        slider.value = settings.SongEditorSettings.MidiVelocity;
        slider.OnValueChangedAsObservable()
            .Subscribe(newValue => settings.SongEditorSettings.MidiVelocity = (int)newValue);
    }
}
