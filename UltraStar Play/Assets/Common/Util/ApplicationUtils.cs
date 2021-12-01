using UnityEngine;

public static class ApplicationUtils
{
    public static void QuitOrStopPlayMode()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public static string GetStreamingAssetsPath(string pathInStreamingAssetsFolder)
    {
#if UNITY_ANDROID
        return AndroidStreamingAssets.Path + "/" + pathInStreamingAssetsFolder;
#else
        return Application.streamingAssetsPath + "/" + pathInStreamingAssetsFolder;
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

    public static bool IsSupportedAudioFormat(string fileExtension)
    {
        fileExtension = NormalizeFileExtension(fileExtension);
        return fileExtension
            is "mp3"
            or "ogg"
            or "wav";
    }

    public static bool IsSupportedVideoFormat(string fileExtension)
    {
        fileExtension = NormalizeFileExtension(fileExtension);
        return fileExtension
            is "avi"
            or "mp4"
            or "mpg"
            or "mpeg"
            or "vp8"
            or "m4v"
            or "mov"
            or "dv"
            or "afs"
            or "wmf";
    }

    private static string NormalizeFileExtension(string fileExtension)
    {
        if (fileExtension == null)
        {
            return "";
        }
        if (fileExtension.StartsWith("."))
        {
            fileExtension = fileExtension.Substring(1);
        }
        return fileExtension.ToLowerInvariant();
    }
}
