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

public class SongEditorRecordingDelayInputField : MonoBehaviour, INeedInjection
{
    [Inject]
    private Settings settings;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private InputField inputField;

    void Start()
    {
        inputField.text = settings.SongEditorSettings.MicDelayInMillis.ToString();
        inputField.OnValueChangedAsObservable().Subscribe(OnTextChanged);
    }

    private void OnTextChanged(string newText)
    {
        int newInt = newText.TryParseAsInteger(settings.SongEditorSettings.MicDelayInMillis);
        settings.SongEditorSettings.MicDelayInMillis = newInt;
    }
}
