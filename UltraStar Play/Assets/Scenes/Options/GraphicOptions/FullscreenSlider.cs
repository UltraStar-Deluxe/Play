using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class FullscreenSlider : TextItemSlider<FullScreenMode>
{

    protected override void Start()
    {
        base.Start();
        Items = EnumUtils.GetValuesAsList<FullScreenMode>();
        if (Application.isEditor)
        {
            Selection.Value = FullScreenMode.Windowed;
        }
        else
        {
            Selection.Value = Screen.fullScreenMode;
        }
        Selection.Subscribe(newValue => SettingsManager.Instance.Settings.GraphicSettings.fullScreenMode = newValue);
    }

}