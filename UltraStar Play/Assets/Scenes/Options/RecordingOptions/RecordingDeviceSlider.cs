using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RecordingDeviceSlider : TextItemSlider<MicProfile>
{
    protected override void Awake()
    {
        base.Awake();
        UpdateItems();
    }

    protected override string GetDisplayString(MicProfile value)
    {
        if (value == null)
        {
            return "";
        }
        else
        {
            return value.Name;
        }
    }

    public void UpdateItems()
    {
        // Create list of connected and loaded microphones without duplicates.
        // A loaded microphone might have been created with hardware that is not connected now.
        List<string> connectedMicNames = Microphone.devices.ToList();
        List<MicProfile> loadedMicProfiles = SettingsManager.Instance.Settings.MicProfiles;
        List<MicProfile> micProfiles = new List<MicProfile>(loadedMicProfiles);

        // Create mic profiles for connected microphones that are not yet in the list
        foreach (string connectedMicName in connectedMicNames)
        {
            bool alreadyInList = micProfiles.AnyMatch(it => it.Name == connectedMicName);
            if (!alreadyInList)
            {
                MicProfile micProfile = new MicProfile(connectedMicName);
                micProfiles.Add(micProfile);
            }
        }

        Items = micProfiles;
    }
}
