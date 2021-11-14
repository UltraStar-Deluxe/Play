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

public class SongEditorMidiPlaybackOffsetInputField : MonoBehaviour, INeedInjection
{
    [Inject]
    private Settings settings;

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private InputField inputField;

    void Start()
    {
        inputField.text = settings.SongEditorSettings.MidiPlaybackOffsetInMillis.ToString();
        inputField.OnValueChangedAsObservable().Subscribe(newText =>
        {
            int newInt = newText.TryParseAsInteger(settings.SongEditorSettings.MidiPlaybackOffsetInMillis);
            settings.SongEditorSettings.MidiPlaybackOffsetInMillis = newInt;
        });
    }
}
