using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class ApplicationUtils
{
    public const string GeneratedFolderName = "Generated";

    public static readonly IReadOnlyList<string> supportedAudioFiles = new List<string>
    {
        "mp3",
        "ogg",
        "wav"
    };

    public static readonly IReadOnlyList<string> supportedVideoFiles = new List<string>
    {
        "avi",
        "mp4",
        "mpg",
        "mpeg",
        "vp8",
        "webm",
        "m4v",
        "mov",
        "dv",
        "afs",
        "wmf",
    };

    public static void OpenDirectory(string path)
    {
        Application.OpenURL("file://" + path);
    }

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

    public static ScreenResolution GetScreenResolution()
    {
        // Screen.currentResolution in window mode returns the size of the desktop, not of the Unity application.
        // Thus, use Screen.width and Screen.height instead, which return the pixel size of the Unity application.
        ScreenResolution res = new(Screen.width, Screen.height, Screen.currentResolution.refreshRate);
        return res;
    }

    public static Vector2 GetScreenSizeInPanelCoordinates(PanelHelper panelHelper)
    {
        return panelHelper.ScreenToPanel(new Vector2(Screen.width, Screen.height));
    }

    public static bool IsSupportedAudioFormat(string fileExtension)
    {
        fileExtension = NormalizeFileExtension(fileExtension);
        return supportedAudioFiles.Contains(fileExtension);
    }

    public static bool IsSupportedVideoFormat(string fileExtension)
    {
        fileExtension = NormalizeFileExtension(fileExtension);
        return supportedVideoFiles.Contains(fileExtension);
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

    public static bool IsLargeScreen()
    {
        return GetPhysicalDiagonalScreenSizeInInches() > 10;
    }

    public static bool IsSmallScreen()
    {
        return !IsLargeScreen();
    }

    public static float GetPhysicalDiagonalScreenSizeInInches()
    {
        // Get diagonal of right-angled triangle via Pythagoras theorem
        float widthInPixels = Screen.width * Screen.width;
        float heightInPixels = Screen.height * Screen.height;
        float diagonalInPixels = Mathf.Sqrt(widthInPixels + heightInPixels);
        float diagonalInInches = diagonalInPixels / Screen.dpi;
        return diagonalInInches;
    }
}
