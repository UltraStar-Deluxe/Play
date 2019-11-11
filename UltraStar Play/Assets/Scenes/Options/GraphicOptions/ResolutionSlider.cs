using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniRx;

public class ResolutionSlider : TextItemSlider<ScreenResolution>
{
    protected override void Start()
    {
        base.Start();
        if (Application.isEditor)
        {
            Items = GetDummyResolutions();
            Selection.Value = Items[0];
        }
        else
        {
            Items = GetResolutions();
            ScreenResolution currentScreenResolution = ApplicationUtils.GetCurrentAppResolution();
            Selection.Value = Items.Where(it => it.Equals(currentScreenResolution))
                .FirstOrDefault().OrIfNull(Items[0]);
        }
        Selection.Subscribe(newValue => SettingsManager.Instance.Settings.GraphicSettings.resolution = newValue);
    }

    protected override string GetDisplayString(ScreenResolution item)
    {
        return $"{item.Width} x {item.Height} ({item.RefreshRate} Hz)";
    }

    private List<ScreenResolution> GetDummyResolutions()
    {
        List<ScreenResolution> result = new List<ScreenResolution>();
        result.Add(CreateResolution(800, 600, 60));
        result.Add(CreateResolution(1024, 768, 60));
        result.Add(CreateResolution(1920, 1080, 60));
        return result;
    }

    private ScreenResolution CreateResolution(int width, int height, int refreshRate)
    {
        ScreenResolution res = new ScreenResolution(width, height, refreshRate);
        return res;
    }

    private List<ScreenResolution> GetResolutions()
    {
        List<ScreenResolution> result = Screen.resolutions.Select(it => new ScreenResolution(it)).ToList();
        return result;
    }
}
