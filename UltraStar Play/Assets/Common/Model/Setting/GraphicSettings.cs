
using System;
using UnityEngine;

[Serializable]
public class GraphicSettings
{
    // Screen.currentResolution may only be called from Start() and Awake(), thus use a dummy here.
    public ScreenResolution resolution = new ScreenResolution(800, 600, 60);
    public FullScreenMode fullScreenMode = FullScreenMode.Windowed;
    public bool useImageAsCursor = true;
}