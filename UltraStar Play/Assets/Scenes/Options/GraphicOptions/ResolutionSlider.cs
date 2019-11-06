using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ResolutionSlider : TextItemSlider<Resolution>
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
            Selection.Value = Items.Where(it => it.Equals(Screen.currentResolution)).FirstOrDefault();
        }
    }

    protected override string GetDisplayString(Resolution item)
    {
        return $"{item.width} x {item.height} ({item.refreshRate} Hz)";
    }

    private List<Resolution> GetDummyResolutions()
    {
        List<Resolution> result = new List<Resolution>();
        result.Add(CreateResolution(800, 600, 60));
        result.Add(CreateResolution(1024, 768, 60));
        result.Add(CreateResolution(1920, 1080, 60));
        return result;
    }

    private Resolution CreateResolution(int v1, int v2, int v3)
    {
        Resolution res = new Resolution();
        res.width = v1;
        res.height = v2;
        res.refreshRate = v3;
        return res;
    }

    private List<Resolution> GetResolutions()
    {
        List<Resolution> result = new List<Resolution>(Screen.resolutions);
        return result;
    }
}
