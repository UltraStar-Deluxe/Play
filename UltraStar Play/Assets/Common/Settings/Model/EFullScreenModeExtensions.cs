using System;
using UnityEngine;

public static class EFullScreenModeExtensions
{
    public static FullScreenMode ToUnityFullScreenMode(this EFullScreenMode fullScreenMode)
    {
        switch (fullScreenMode)
        {
            case EFullScreenMode.ExclusiveFullScreen: return FullScreenMode.ExclusiveFullScreen;
            case EFullScreenMode.FullScreenWindow: return FullScreenMode.FullScreenWindow;
            case EFullScreenMode.MaximizedWindow:  return FullScreenMode.MaximizedWindow;
            case EFullScreenMode.Windowed:  return FullScreenMode.Windowed;
            default:
                throw new ArgumentOutOfRangeException(nameof(fullScreenMode), fullScreenMode, null);
        }
    }

    public static EFullScreenMode ToCustomFullScreenMode(this FullScreenMode fullScreenMode)
    {
        switch (fullScreenMode)
        {
            case FullScreenMode.ExclusiveFullScreen: return EFullScreenMode.ExclusiveFullScreen;
            case FullScreenMode.FullScreenWindow: return EFullScreenMode.FullScreenWindow;
            case FullScreenMode.MaximizedWindow:  return EFullScreenMode.MaximizedWindow;
            case FullScreenMode.Windowed:  return EFullScreenMode.Windowed;
            default:
                throw new ArgumentOutOfRangeException(nameof(fullScreenMode), fullScreenMode, null);
        }
    }
}
