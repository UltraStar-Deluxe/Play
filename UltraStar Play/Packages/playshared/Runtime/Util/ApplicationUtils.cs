using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ApplicationUtils
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        // Unity API only allows to query these properties on the main thread.
        threadSafePersistentDataPath = Application.persistentDataPath;
        threadSafeStreamingAssetsPath = Application.streamingAssetsPath;
        threadSafeTemporaryCachePath = Application.temporaryCachePath;
    }

    public static int CurrentFrameRate => Math.Max(1, Application.targetFrameRate > 0
        // The target frame rate is used
        ? Application.targetFrameRate
        // The refresh rate of the monitor is used
        : (int)Screen.currentResolution.refreshRateRatio.value);

    public const string GeneratedFolderName = "Generated";

    private static bool useVlcToPlayMediaFiles;
    public static bool UseVlcToPlayMediaFiles
    {
        get => useVlcToPlayMediaFiles;
        set
        {
            if (useVlcToPlayMediaFiles == value)
            {
                return;
            }
            Debug.Log($"use vlc to play media files: {value}");
            useVlcToPlayMediaFiles = value;
            supportedAudioFiles = GetSupportedAudioFiles(useVlcToPlayMediaFiles);
            supportedVideoFiles = GetSupportedVideoFiles(useVlcToPlayMediaFiles);
        }
    }

    private static string threadSafeStreamingAssetsPath;
    public static string ThreadSafeStreamingAssetsPath
    {
        get
        {
            if (ThreadUtils.IsMainThread())
            {
                return Application.streamingAssetsPath;
            }

            if (threadSafeStreamingAssetsPath.IsNullOrEmpty())
            {
                throw new Exception("StreamingAssetsPath has not been stored yet for thread-safe usage");
            }
            return threadSafeStreamingAssetsPath;
        }
    }

    private static string threadSafeTemporaryCachePath;
    public static string ThreadSafeTemporaryCachePath
    {
        get
        {
            if (ThreadUtils.IsMainThread())
            {
                return Application.temporaryCachePath;
            }

            if (threadSafeTemporaryCachePath.IsNullOrEmpty())
            {
                throw new Exception("TemporaryCachePath has not been stored yet for thread-safe usage");
            }
            return threadSafeTemporaryCachePath;
        }
    }

    private static string threadSafePersistentDataPath;
    public static string ThreadSafePersistentDataPath
    {
        get
        {
            if (ThreadUtils.IsMainThread())
            {
                return Application.persistentDataPath;
            }

            if (threadSafePersistentDataPath.IsNullOrEmpty())
            {
                throw new Exception("PersistentDataPath has not been stored yet for thread-safe usage");
            }
            return threadSafePersistentDataPath;
        }
    }

    public static string PlaylistFolder => GetPersistentDataPath("Playlists");
    public static string FavoritesPlaylistFilePath => $"{PlaylistFolder}/{FavoritesPlaylistName}.{UltraStarPlaylistFileExtension}";
    public const string FavoritesPlaylistName = "Favorites";

    public const string UltraStarPlaylistFileExtension = "upl";
    public const string M3uPlaylistFileExtension = "m3u";

    public static readonly IReadOnlyCollection<string> supportedSoundfontFiles = new HashSet<string>
    {
        "sf2",
    };

    public static readonly IReadOnlyCollection<string> supportedImageFiles = new HashSet<string>
    {
        "png",
        "jpg",
        "jpeg",
    };

    public static readonly IReadOnlyCollection<string> supportedMidiFiles = new HashSet<string>
    {
        "mid",
        "midi",
        "kar",
    };

    public static readonly IReadOnlyCollection<string> audioFileExtensions = ReadAudioFileExtensionsFromFile()
        .Select(line => line.Trim().TrimStart('.'))
        .Where(line => !line.IsNullOrEmpty())
        .ToHashSet();

    // Supported file formats of ffmpeg can be obtained via "ffmpeg -demuxers"
    // See also https://stackoverflow.com/questions/50069235/what-are-all-of-the-file-extensions-supported-by-ffmpeg
    // See also http://www.ffmpeg.org/general.html#toc-Supported-File-Formats_002c-Codecs-or-Features
    public static readonly IReadOnlyCollection<string> vlcSupportedFileExtensions = ReadFfmpegSupportedFileExtensionsFromFile()
        .Select(line => line.Trim().TrimStart('.'))
        .Where(line => !line.IsNullOrEmpty())
        .ToHashSet();

    public static readonly IReadOnlyCollection<string> vlcSupportedAudioFiles = vlcSupportedFileExtensions
        .Where(fileExtension => audioFileExtensions.Contains(fileExtension))
        .ToList();

    public static readonly IReadOnlyCollection<string> vlcSupportedVideoFiles = vlcSupportedFileExtensions
        .Where(fileExtension => !audioFileExtensions.Contains(fileExtension))
        .ToList();

    public static readonly IReadOnlyCollection<string> unitySupportedAudioFiles = new HashSet<string>
    {
        "mp3",
        "ogg",
        "wav",
    }.ToHashSet();

    public static IReadOnlyCollection<string> supportedAudioFiles = GetSupportedAudioFiles(false);

    public static readonly IReadOnlyCollection<string> supportedVocalsSeparationAudioFiles = new HashSet<string>
    {
        "wav",
        "mp3",
        "ogg",
        "m4a",
        "wma",
        "flac",
    }.ToHashSet();

    public static readonly IReadOnlyCollection<string> supportedBasicPitchDetectionAudioFiles = new HashSet<string>
    {
        "wav",
        "mp3",
        "ogg",
    }.ToHashSet();

    public static readonly IReadOnlyCollection<string> unitySupportedVideoFiles = new HashSet<string>
    {
        // See https://docs.unity3d.com/Manual/VideoSources-FileCompatibility.html#CompatibilityWithTargetPlatforms
#if !UNITY_STANDALONE_LINUX
        "mp4",
#endif
        "avi",
        "mpg",

        // NOTE: webm is only supported by Unity when using VP8.
        // webm with VP9 is not supported by Unity and will fail, such that ffmpeg or similar should be used as fallback.
        "webm",
    };

    public static IReadOnlyCollection<string> supportedVideoFiles = GetSupportedVideoFiles(false);

    public static void OpenDirectory(string path)
    {
        OpenUrl("file://" + path);
    }

    public static void OpenUrl(string url)
    {
        Debug.Log($"Open url: {url}");
        ThreadUtils.RunOnMainThread(() => Application.OpenURL(url));
    }

    public static void QuitOrStopPlayMode()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public static string GetStreamingAssetsPath(string pathInStreamingAssetsFolder)
    {
#if UNITY_ANDROID
        return $"{AndroidStreamingAssets.Path}/{pathInStreamingAssetsFolder}";
#else
        return $"{ThreadSafeStreamingAssetsPath}/{pathInStreamingAssetsFolder}";
#endif
    }

    public static string GetPersistentDataPath(string pathInPersistentDataFolder)
    {
        return $"{ThreadSafePersistentDataPath}/{pathInPersistentDataFolder}";
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

    public static bool IsUnitySupportedAudioFormat(string fileExtension)
    {
        fileExtension = NormalizeFileExtension(fileExtension);
        return unitySupportedAudioFiles.Contains(fileExtension);
    }

    public static bool IsVlcSupportedAudioFormat(string fileExtension)
    {
        fileExtension = NormalizeFileExtension(fileExtension);
        return vlcSupportedAudioFiles.Contains(fileExtension);
    }

    public static bool IsVlcSupportedVideoFormat(string fileExtension)
    {
        fileExtension = NormalizeFileExtension(fileExtension);
        return vlcSupportedVideoFiles.Contains(fileExtension);
    }

    public static bool IsSupportedAudioFormat(string fileExtension)
    {
        fileExtension = NormalizeFileExtension(fileExtension);
        return supportedAudioFiles.Contains(fileExtension);
    }

    public static bool IsUnitySupportedVideoFormat(string fileExtension)
    {
        fileExtension = NormalizeFileExtension(fileExtension);
        return unitySupportedVideoFiles.Contains(fileExtension);
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

    public static bool IsSupportedBasicPitchDetectionAudioFormat(string fileExtension)
    {
        fileExtension = NormalizeFileExtension(fileExtension);
        return supportedBasicPitchDetectionAudioFiles.Contains(fileExtension);
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
            string internalStorageTranslation = Translation.Get("androidInternalStorage");
            string sdCardStorageTranslation = Translation.Get("androidSdCardStorage");

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

    public static string GetDemoSongFolderAbsolutePath()
    {
        return GetStreamingAssetsPath("DemoSongs");
    }

    public static bool IsGeneratedAudioFile(string audioFile)
    {
        // Audio separation creates files called "vocals.ogg" and "instrumental.ogg"
        return Path.GetFileName(audioFile) == "vocals"
               || Path.GetFileName(audioFile) == "instrumental";
    }

    public static void SetUsePortAudio(bool preferPortAudio)
    {
        IMicrophoneAdapter.Instance.UsePortAudio = preferPortAudio && CanUsePortAudio();
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

    private static IReadOnlyCollection<string> GetSupportedAudioFiles(bool includeVlcFormats)
    {
        return unitySupportedAudioFiles
            .Union(supportedMidiFiles)
            .Union(includeVlcFormats ? vlcSupportedAudioFiles : new List<string>())
            .ToHashSet();
    }

    private static IReadOnlyCollection<string> GetSupportedVideoFiles(bool includeVlcFormats)
    {
        return unitySupportedVideoFiles
            .Union(includeVlcFormats ? vlcSupportedVideoFiles : new List<string>())
            .ToHashSet();
    }

    public static string GetTemporaryCachePath(string pathInsideTemporaryCachePath)
    {
        return $"{ThreadSafeTemporaryCachePath}/{pathInsideTemporaryCachePath}";
    }

    private static IReadOnlyCollection<string> ReadAudioFileExtensionsFromFile()
    {
        try
        {
            return File.ReadAllLines(GetStreamingAssetsPath("audio-file-extensions.txt"), Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);

            List<string> fallbackList = new List<string>() { "ogg", "mp3", "wav" };
            Debug.LogError($"Failed to load audio file extensions from file. Using fallback list: {fallbackList.JoinWith(", ")}");
            return fallbackList;
        }
    }

    private static IReadOnlyCollection<string> ReadFfmpegSupportedFileExtensionsFromFile()
    {
        try
        {
            return File.ReadAllLines(GetStreamingAssetsPath("ffmpeg-supported-common-file-extensions.txt"), Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);

            List<string> fallbackList = new List<string>() { "ogg", "mp3", "wav" };
            Debug.LogError($"Failed to load ffmpeg supported audio file extensions from file. Using fallback list: {fallbackList.JoinWith(", ")}");
            return fallbackList;
        }
    }
}
