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

public class ScreenResolutionPickerControl : LabeledItemPickerControl<ScreenResolution>
{
    public ScreenResolutionPickerControl(ItemPicker itemPicker)
        : base(itemPicker, GetItems())
    {
        GetLabelTextFunction = item => $"{item.Width} x {item.Height} ({item.RefreshRate} Hz)";
        if (Application.isEditor)
        {
            Selection.Value = Items[0];
        }
        else
        {
            ScreenResolution currentScreenResolution = ApplicationUtils.GetCurrentAppResolution();
            Selection.Value = Items.FirstOrDefault(it => it.Equals(currentScreenResolution))
                .OrIfNull(Items[0]);
        }
        Selection.Subscribe(newValue => SettingsManager.Instance.Settings.GraphicSettings.resolution = newValue);
    }

    private static List<ScreenResolution> GetItems()
    {
        if (Application.isEditor)
        {
            return GetDummyResolutions();
        }
        else
        {
            return GetResolutions();
        }
    }

    private static List<ScreenResolution> GetDummyResolutions()
    {
        List<ScreenResolution> result = new List<ScreenResolution>();
        result.Add(CreateResolution(800, 600, 60));
        result.Add(CreateResolution(1024, 768, 60));
        result.Add(CreateResolution(1920, 1080, 60));
        return result;
    }

    private static ScreenResolution CreateResolution(int width, int height, int refreshRate)
    {
        ScreenResolution res = new ScreenResolution(width, height, refreshRate);
        return res;
    }

    private static List<ScreenResolution> GetResolutions()
    {
        List<ScreenResolution> result = Screen.resolutions.Select(it => new ScreenResolution(it)).ToList();
        return result;
    }
}
