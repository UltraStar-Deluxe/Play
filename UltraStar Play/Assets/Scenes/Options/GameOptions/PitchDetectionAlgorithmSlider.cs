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

public class PitchDetectionAlgorithmSlider : TextItemSlider<EPitchDetectionAlgorithm>
{
    protected override void Start()
    {
        base.Start();
        Items = EnumUtils.GetValuesAsList<EPitchDetectionAlgorithm>();
        Selection.Value = SettingsManager.Instance.Settings.AudioSettings.pitchDetectionAlgorithm;
        Selection.Subscribe(newValue => SettingsManager.Instance.Settings.AudioSettings.pitchDetectionAlgorithm = newValue);
    }

    protected override string GetDisplayString(EPitchDetectionAlgorithm item)
    {
        switch (item)
        {
            case EPitchDetectionAlgorithm.Dywa:
                return "Dynamic Wavelet (default)";
            case EPitchDetectionAlgorithm.Camd:
                return "Circular Average Magnitude Difference";
            default:
                return item.ToString();
        }
    }
}