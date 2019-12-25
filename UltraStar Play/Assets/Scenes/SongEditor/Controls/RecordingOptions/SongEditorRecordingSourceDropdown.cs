using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using static UnityEngine.UI.Dropdown;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

[RequireComponent(typeof(Dropdown))]
public class SongEditorRecordingSourceDropdown : MonoBehaviour, INeedInjection
{
    [Inject(searchMethod = SearchMethods.GetComponent)]
    private Dropdown dropdown;

    [Inject]
    private Settings settings;

    private List<ESongEditorRecordingSource> itemsInDropdown;

    void Start()
    {
        itemsInDropdown = EnumUtils.GetValuesAsList<ESongEditorRecordingSource>();
        dropdown.options = itemsInDropdown.Select(it => new OptionData(it.ToString())).ToList();
        dropdown.OnValueChangedAsObservable().Subscribe(OnDropdownValueChanged);

        int index = itemsInDropdown.IndexOf(settings.SongEditorSettings.RecordingSource);
        dropdown.value = index;
    }

    private void OnDropdownValueChanged(int index)
    {
        if (index > 0 && index < itemsInDropdown.Count)
        {
            ESongEditorRecordingSource selectedItem = itemsInDropdown[index];
            settings.SongEditorSettings.RecordingSource = selectedItem;
        }
    }
}

