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

public class SaveBackupFileToggle : MonoBehaviour, INeedInjection
{
    [Inject]
    private Settings settings;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private Toggle toggle;

    void Start()
    {
        toggle.isOn = settings.SongEditorSettings.SaveCopyOfOriginalFile;
        toggle.OnValueChangedAsObservable().Subscribe(OnToggleValueChanged);
    }

    private void OnToggleValueChanged(bool newValue)
    {
        settings.SongEditorSettings.SaveCopyOfOriginalFile = newValue;
    }
}
