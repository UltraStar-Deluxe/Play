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

public class TargetFpsSlider : TextItemSlider<int>, INeedInjection
{
    [Inject]
    private Settings settings;

    protected override void Start()
    {
        base.Start();
        Items = new List<int> { 30, 60, -1 };
        Selection.Value = settings.GraphicSettings.targetFps;
        Selection.Subscribe(newValue => settings.GraphicSettings.targetFps = newValue);
    }

    protected override string GetDisplayString(int value)
    {
        if (value < 0)
        {
            if (PlatformUtils.IsStandalone)
            {
                // On standalone platforms the default frame rate is the maximum achievable frame rate.
                return "Unlimited";
            }
            else
            {
                // All mobile platforms have a fix cap for their maximum achievable frame rate,
                // that is equal to the refresh rate of the screen (60 Hz = 60 fps, 40 Hz = 40 fps, ...).
                // Screen.currentResolution contains the screen's refresh rate.
                return Screen.currentResolution.refreshRate.ToString();
            }
        }
        return value.ToString();
    }
}
