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

public class PreviewVolumeSlider : NumberSpinner
{
    protected override void Start()
    {
        base.Start();

        hasMax = true;
        hasMin = true;
        minValue = 0;
        maxValue = 100;
        step = 5;
        formatString = "F0";
        SelectedValue = SettingsManager.Instance.Settings.AudioSettings.PreviewVolumePercent;
        SelectedValueStream.Subscribe(newValue => SettingsManager.Instance.Settings.AudioSettings.PreviewVolumePercent = (int)newValue);
    }

    protected override void UpdateUiText(double newValue)
    {
        if (newValue > 0)
        {
            uiText.text = newValue.ToString(formatString) + " %";
        }
        else
        {
            uiText.text = "No preview";
        }
    }
}
