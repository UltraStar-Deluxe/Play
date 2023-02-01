using System;
using System.Collections.Generic;
using UnityEngine;

// The Resolution type of Unity is not serialized for some reason.
// Thus, use this wrapper instead.
public struct ScreenResolution : IEqualityComparer<ScreenResolution>
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int RefreshRate { get; set; }

    public ScreenResolution(int widht, int height, int refreshRate)
    {
        this.Width = widht;
        this.Height = height;
        this.RefreshRate = refreshRate;
    }

    public ScreenResolution(Resolution res) : this(res.width, res.height, res.refreshRate)
    {

    }

    public override string ToString()
    {
        return $"{Width} x {Height} @ {RefreshRate} Hz";
    }

    public bool Equals(ScreenResolution x, ScreenResolution y)
    {
        return x.Width == y.Width && x.Height == y.Height && x.RefreshRate == y.RefreshRate;
    }

    public int GetHashCode(ScreenResolution obj)
    {
        return HashCode.Combine(obj.Width, obj.Height, obj.RefreshRate);
    }
}
