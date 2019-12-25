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

[RequireComponent(typeof(Toggle))]
public class SongEditorLayerToggle : MonoBehaviour, INeedInjection
{
    public ESongEditorLayer layer;

    [Inject]
    private SongEditorLayerManager songEditorLayerManager;

    [Inject(searchMethod = SearchMethods.GetComponent)]
    private Toggle toggle;

    void Start()
    {
        toggle.isOn = songEditorLayerManager.IsLayerEnabled(layer);
        toggle.OnValueChangedAsObservable()
            .Subscribe(newValue => songEditorLayerManager.SetLayerEnabled(layer, newValue));
    }
}
