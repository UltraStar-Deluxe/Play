
using System;
using UnityEngine;

[Serializable]
public class GraphicSettings
{
    public Resolution resolution = GetResolution();
    public FullScreenMode fullScreenMode = FullScreenMode.Windowed;
    public bool useImageAsCursor = true;

    private static Resolution GetResolution()
    {
        if (Application.isEditor)
        {
            return GetDummyResolutions();
        }
        else
        {
            return Screen.currentResolution;
        }
    }

    private static Resolution GetDummyResolutions()
    {
        Resolution res = new Resolution();
        res.width = 1024;
        res.height = 764;
        res.refreshRate = 60;
        return res;
    }
}