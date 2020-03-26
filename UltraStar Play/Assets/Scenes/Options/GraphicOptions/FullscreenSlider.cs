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
            // The full-screen mode can change, e.g., via global keyboard shortcut. Thus, synchronize with the settings.
            SettingsManager.Instance.Settings.GraphicSettings.ObserveEveryValueChanged(it => it.fullScreenMode)
                .Subscribe(newFullScreenMode =>
                {
                    // Avoid infinite recursion.
                    if (newFullScreenMode != Selection.Value)
                    {
                        Selection.Value = newFullScreenMode;
                    }
                });
        }
        Selection.Subscribe(newValue => SettingsManager.Instance.Settings.GraphicSettings.fullScreenMode = newValue);
    }

}
