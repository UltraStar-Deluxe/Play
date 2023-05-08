using System;
using System.Collections.Generic;
using System.IO;
using ProTrans;
using System.Linq;
using PortAudioForUnity;
using UnityEngine;

public static class ApplicationUtils
{
    public const string GeneratedFolderName = "Generated";

    public static readonly IReadOnlyList<string> supportedSoundfontFiles = new List<string>
    {
        "sf2",
    };
    
    public static readonly IReadOnlyList<string> supportedImageFiles = new List<string>
    {
        "png",
        "jpg",
        "jpeg",
    };

    public static readonly IReadOnlyList<string> supportedMidiFiles = new List<string>
    {
        "mid",
        "midi",
        "kar",
    };
    
    public static readonly IReadOnlyList<string> supportedAudioFiles = new List<string>
    {
        "mp3",
        "ogg",
        "wav"
    }.Union(supportedMidiFiles).ToList();

    public static readonly IReadOnlyList<string> supportedVocalsSeparationAudioFiles = new List<string>
    {
        "wav",
        "mp3",
        "ogg",
        "m4a",
        "wma",
        "flac",
    }.Intersect(supportedAudioFiles).ToList();

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

    public static string GetPersistentDataPath(string pathInPersistentDataFolder)
    {
        return Application.persistentDataPath + "/" + pathInPersistentDataFolder;
    }
    
    public static Vector2 GetScreenSize()
    {
        return new Vector2(Screen.width, Screen.height);
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
        return panelHelper.GetScreenSizeInPanelCoordinates();
    }

    public static bool IsSupportedImageFormat(string fileExtension)
    {
        fileExtension = NormalizeFileExtension(fileExtension);
        return supportedImageFiles.Contains(fileExtension);
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

    public static bool IsSupportedMidiFormat(string fileExtension)
    {
        fileExtension = NormalizeFileExtension(fileExtension);
        return supportedMidiFiles.Contains(fileExtension);
    }
    
    public static bool IsSupportedVocalsSeparationAudioFormat(string fileExtension)
    {
        fileExtension = NormalizeFileExtension(fileExtension);
        return supportedVocalsSeparationAudioFiles.Contains(fileExtension);
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

    public static string ReplacePathsWithDisplayString(string text)
    {
        if (PlatformUtils.IsAndroid)
        {
            string internalStorageTranslation = "Internal Storage";
            string sdCardStorageTranslation = "SD Card";
            if (ThreadUtils.IsMainThread())
            {
                // The ProTrans TranslationManager only works on the main thread.
                internalStorageTranslation = TranslationManager.GetTranslation("androidInternalStorage");
                sdCardStorageTranslation = TranslationManager.GetTranslation("androidSdCardStorage");
            }

            string internalStorageRoot = AndroidUtils.GetStorageRootPath(false);
            string sdCardStorageRoot = AndroidUtils.GetStorageRootPath(true);
            return text
                .Replace(internalStorageRoot, $"{internalStorageTranslation}/")
                .Replace(sdCardStorageRoot, $"{sdCardStorageTranslation}/");
        }

        return text;
    }
    
    public static string GetGeneratedOutputFolderForSourceFilePath(string generatedFolderBasePath, string sourceFilePath)
    {
        // Include hash code of file path in the generated folder name
        int sourceFilePathHash = new FileInfo(sourceFilePath).FullName.GetHashCode();
        string sourceFilePathHashHex = Convert.ToString(sourceFilePathHash, 16);
        string sourceFileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceFilePath);
        string generatedFolderName = $"{sourceFileNameWithoutExtension}__{sourceFilePathHashHex}";

        string generatedOutputFolder = generatedFolderBasePath + $"/{generatedFolderName}";
        return generatedOutputFolder;
    }

    public static string GetGeneratedSongFolderAbsolutePath()
    {
        return Application.persistentDataPath + $"/{GeneratedFolderName}/Songs";
    }

    public static bool IsGeneratedAudioFile(string audioFile)
    {
        // Audio separation creates files called "vocals.ogg" and "instrumental.ogg"
        return Path.GetFileName(audioFile) == "vocals"
               || Path.GetFileName(audioFile) == "instrumental";
    }

    public static void SetUsePortAudio(bool preferPortAudio)
    {
        MicrophoneAdapter.UsePortAudio = preferPortAudio && CanUsePortAudio();
    }

    public static bool CanUsePortAudio()
    {
        // TODO: Build PortAudio for Linux and macOS and include the compiled libs in PortAudioForUnity.
        return PlatformUtils.IsWindows;
    }
    
    public static string GetVideoPlayerUri(string uri)
    {
        // Unity on Android MUST NOT use the file:// scheme for vp8/webm files.
        // See https://forum.unity.com/threads/videoplayer-url-issue-with-vp8-webm-on-android-androidvideomedia-error-opening-extractor-10002.1255434/#post-7978743
#if UNITY_ANDROID
        if (uri.StartsWith("file://") && (uri.EndsWith(".vp8") || uri.EndsWith(".webm")))
        {
            return uri.Substring("file://".Length);
        }
#endif
        return uri;
    }
}
