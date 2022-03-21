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

public class FullscreenModePickerControl : LabeledItemPickerControl<FullScreenMode>
{
    public FullscreenModePickerControl(ItemPicker itemPicker, GameObject gameObject)
        : base(itemPicker, EnumUtils.GetValuesAsList<FullScreenMode>())
    {
        if (Application.isEditor)
        {
            Selection.Value = FullScreenMode.Windowed;
        }
        else
        {
            Selection.Value = Screen.fullScreenMode;
            // The full-screen mode can change, e.g., via global keyboard shortcut. Thus, synchronize with the settings.
            SettingsManager.Instance.Settings.GraphicSettings
                .ObserveEveryValueChanged(it => it.fullScreenMode)
                .Subscribe(newFullScreenMode =>
                {
                    // Avoid infinite recursion.
                    if (newFullScreenMode != Selection.Value)
                    {
                        Selection.Value = newFullScreenMode;
                    }
                })
                .AddTo(gameObject);
        }
        Selection.Subscribe(newValue => SettingsManager.Instance.Settings.GraphicSettings.fullScreenMode = newValue);
    }
}
