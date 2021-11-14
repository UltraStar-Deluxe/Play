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

public class SongEditorMidiSoundPlayAlongToggle : MonoBehaviour, INeedInjection
{
    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private Toggle toggle;

    [Inject]
    private Settings settings;

    void Start()
    {
        toggle.isOn = settings.SongEditorSettings.MidiSoundPlayAlongEnabled;
        toggle.OnValueChangedAsObservable()
            .Subscribe(newValue => settings.SongEditorSettings.MidiSoundPlayAlongEnabled = newValue);
    }
}
