using System;
using UnityEngine;

// HSV / HSB Color
[Serializable]
public struct HsvColor
{
    // Hue
    public float H { get; private set; }
    // Saturation
    public float S { get; private set; }
    // Value / Brightness
    public float V { get; private set; }

    public HsvColor(Color rgbColor)
    {
        Color.RGBToHSV(rgbColor, out float h, out float s, out float v);
        this.H = h;
        this.S = s;
        this.V = v;
    }

    public HsvColor(float h, float s, float v)
    {
        this.H = h;
        this.S = s;
        this.V = v;
    }

    public Color ToRgbColor()
    {
        return Color.HSVToRGB(H, S, V);
    }
}