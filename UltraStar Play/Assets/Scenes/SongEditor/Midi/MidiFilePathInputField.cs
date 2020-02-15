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

public class MidiFilePathInputField : MonoBehaviour, INeedInjection
{
    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private InputField inputField;

    [Inject]
    private Settings settings;

    void Start()
    {
        inputField.text = settings.SongEditorSettings.MidiFilePath;
        inputField.OnValueChangedAsObservable()
            .Subscribe(newValue => settings.SongEditorSettings.MidiFilePath = newValue);
    }
}
