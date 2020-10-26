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

public class ShowFpsSlider : BoolItemSlider, INeedInjection
{
    [Inject]
    private Settings settings;

    [Inject]
    private UiManager uiManager;

    protected override void Start()
    {
        base.Start();
        Selection.Value = settings.DeveloperSettings.showFps;
        Selection.Subscribe(newValue =>
        {
            settings.DeveloperSettings.showFps = newValue;

            if (newValue)
            {
                uiManager.CreateShowFpsInstance();
            }
            else
            {
                uiManager.DestroyShowFpsInstance();
            }
        });
    }
}
