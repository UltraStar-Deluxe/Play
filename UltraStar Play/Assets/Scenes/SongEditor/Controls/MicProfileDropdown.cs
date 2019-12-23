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

public class MicProfileDropdown : MonoBehaviour, INeedInjection
{

    [Inject(searchMethod = SearchMethods.GetComponent)]
    private Dropdown dropdown;

    [Inject]
    private Settings settings;

    [Inject]
    private MicrophonePitchTracker microphonePitchTracker;

    private List<MicProfile> micProfilesInDropdown;

    void Start()
    {
        micProfilesInDropdown = settings.MicProfiles.Where(it => it.IsEnabled && it.IsConnected).ToList();
        dropdown.options = micProfilesInDropdown.Select(it => new OptionData(it.Name)).ToList();
        dropdown.OnValueChangedAsObservable().Subscribe(OnDropdownValueChanged);

        if (micProfilesInDropdown.Count > 0)
        {
            microphonePitchTracker.MicProfile = micProfilesInDropdown[0];
        }
    }

    private void OnDropdownValueChanged(int index)
    {
        if (index > 0 && index < micProfilesInDropdown.Count)
        {
            MicProfile selectedMicProfile = micProfilesInDropdown[index];
            microphonePitchTracker.MicProfile = selectedMicProfile;
        }
    }
}
