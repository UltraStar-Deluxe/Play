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

public class AdjustFollowingNotesToggle : MonoBehaviour, INeedInjection
{

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private Toggle toggle;

    [Inject]
    private Settings settings;

    void Start()
    {
        toggle.isOn = settings.SongEditorSettings.AdjustFollowingNotes;
        toggle.OnValueChangedAsObservable().Subscribe(OnToggleValueChanged);
    }

    private void OnToggleValueChanged(bool newValue)
    {
        settings.SongEditorSettings.AdjustFollowingNotes = newValue;
    }
}
