using UnityEngine;

public static class ApplicationUtils
{

    public static string AppVersion
    {
        get
        {
            return "0.1.0";
        }
    }

    public static void QuitOrStopPlayMode()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // This method uses Screen.currentResolution,
    // which may only be called from an Awake() or Start() method on the main thread.
    public static ScreenResolution GetCurrentAppResolution()
    {
        // Screen.currentResolution in window mode returns the size of the desktop, not of the Unity application.
        // Thus, use Screen.width and Screen.height instead, which return the pixel size of the Unity application.
        ScreenResolution res = new ScreenResolution(Screen.width, Screen.height, Screen.currentResolution.refreshRate);
        return res;
    }
}