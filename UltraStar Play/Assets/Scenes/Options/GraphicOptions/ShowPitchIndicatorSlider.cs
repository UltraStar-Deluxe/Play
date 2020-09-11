using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

public class ShowPitchIndicatorSlider : BoolItemSlider
{
    protected override void Start()
    {
        base.Start();
        Selection.Value = SettingsManager.Instance.Settings.GraphicSettings.showPitchIndicator;
        Selection.Subscribe(newValue => SettingsManager.Instance.Settings.GraphicSettings.showPitchIndicator = newValue);
    }
}
