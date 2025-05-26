using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using ICSharpCode.SharpZipLib.Zip;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class BuildUtils
{
    private static readonly Dictionary<string, string> copyFilesBeforeBuild = new()
    {
        { "Assets/StreamingAssets/HOW_TO_DOWNLOAD_SPEECH_RECOGNITION_MODELS.txt" , "Assets/StreamingAssets/SpeechRecognitionModels/HOW_TO_DOWNLOAD_SPEECH_RECOGNITION_MODELS.txt" },
    };

    public static void PerformCustomBuild(CustomBuildOptions options)
    {
        CopyFilesBeforeBuild();

        bool isMobileBuild = MobileBuildUtils.IsMobileBuild(options.buildTarget);
        try
        {
            if (isMobileBuild)
            {
                MobileBuildUtils.ExcludeAssetsBeforeMobileBuild();
            }

            AssetDatabase.Refresh();
            DoPerformCustomBuild(options);
        }
        finally
        {
            if (isMobileBuild)
            {
                MobileBuildUtils.IncludeAssetsAfterMobileBuild();
            }
        }
    }

    private static void DoPerformCustomBuild(CustomBuildOptions options)
    {
        ConfigureBuildSettings(options);

        RunUnityBuildPipeline(options);

        if (ShouldCompressOutputFolderToZip(options))
        {
            CompressOutputFolderToZipFile(options);
        }
    }

    private static void RunUnityBuildPipeline(CustomBuildOptions options)
    {
        // Define build output location
        string outputFolderPath = GetBuildOutputFolderFromBuildOptions(options);
        string executableName = GetExecutableName(options.appName, options.buildTarget, options.buildAppBundleForGooglePlay, options.configureKeystoreForAndroidBuild);
        string executableFileInOutputFolder = !executableName.IsNullOrEmpty() ? $"/{executableName}" : "";
        string fullOutputPath = $"{outputFolderPath}{executableFileInOutputFolder}";
        if (options.buildTarget == BuildTarget.StandaloneOSX)
        {
            fullOutputPath += ".app";
        }
        Debug.Log($"Starting build of {options.appName} for {options.buildTarget}. Build options: {options.buildOptions}. Target path: {Path.GetFullPath(fullOutputPath)}");

        // Run Unity build
        string[] enabledScenePaths = GetEnabledScenePaths();
        BuildReport buildReport = BuildPipeline.BuildPlayer(enabledScenePaths, fullOutputPath, options.buildTarget, options.buildOptions);

        // Log results
        LogType logType = GetLogType(buildReport.summary.result);
        TimeSpan buildDuration = buildReport.summary.buildEndedAt - buildReport.summary.buildStartedAt;
        Debug.unityLogger.Log(logType, $"Built {options.appName} for {options.buildTarget}. Build options: {options.buildOptions}. Target path: {Path.GetFullPath(fullOutputPath)}. Duration: {buildDuration.TotalSeconds} seconds");
    }

    private static void ConfigureBuildSettings(CustomBuildOptions options)
    {
        if (options.buildTarget is BuildTarget.Android)
        {
            MobileBuildUtils.ConfigureAndroidBuildSettings(options);
        }
        else if (options.buildTarget is BuildTarget.iOS)
        {
            MobileBuildUtils.ConfigureIosBuildSettings(options);
        }
    }

    private static bool ShouldCompressOutputFolderToZip(CustomBuildOptions options)
    {
        return options.compressOutputFolderToZipFile
               && options.buildTarget
                   is BuildTarget.StandaloneOSX
                   or BuildTarget.StandaloneLinux64
                   or BuildTarget.StandaloneWindows64
                   or BuildTarget.StandaloneWindows;
    }

    private static void CompressOutputFolderToZipFile(CustomBuildOptions options)
    {
        string outputFolderPath = GetBuildOutputFolderFromBuildOptions(options);
        if (options.buildTarget is BuildTarget.StandaloneOSX)
        {
            // A folder that ends with ".app" was created. This folder is the app for macOS.
            // To include this ".app" folder in the ZIP file, we need to move it to a subfolder.
            // Example: generated folder is "MyApp-macOS.app". This will be moved to "MyApp-macOS/MyApp-macOS.app".
            string generatedFolderPath = $"{outputFolderPath}.app";
            string generatedFolderName = Path.GetFileName(generatedFolderPath);
            string subfolderPath = outputFolderPath + $"/{generatedFolderName}";
            if (!Directory.Exists(outputFolderPath))
            {
                Directory.CreateDirectory(outputFolderPath);
            }

            Directory.Move(generatedFolderPath, subfolderPath);
            Debug.Log($"Moved folder {generatedFolderPath} to {subfolderPath}");
        }

        CompressDirectoryToZipFile(outputFolderPath, outputFolderPath + ".zip");
    }

    /**
     * Use the Unix time in minutes as bundle version.
     * This ensures that the value is incremented for every new build.
     */
    public static int GetBundleVersionFromCurrentTime()
    {
        // Using minutes (instead of milliseconds) makes the value small enough to fit into an int32.
        return (int)(TimeUtils.GetUnixTimeMilliseconds() / 1000 / 60);
    }

    private static void CopyFilesBeforeBuild()
    {
        foreach (KeyValuePair<string, string> entry in copyFilesBeforeBuild)
        {
            Debug.Log($"Copy file before build: {entry.Key} -> {entry.Value}");
            if (!FileUtils.Exists(entry.Key))
            {
                Debug.LogWarning("Cannot copy file. File does not exist: " + entry.Key);
                continue;
            }

            DirectoryUtils.CreateDirectory(new FileInfo(entry.Value).Directory.FullName);
            FileUtils.MoveFileOverwriteIfExists(entry.Key, entry.Value);
            if (!FileUtils.Exists(entry.Value))
            {
                throw new Exception($"Failed to copy {entry.Key} to {entry.Value}");
            }
        }

        // Wait for file operations to complete.
        Thread.Sleep(100);
    }

    public static string GetPlayerSettingsFileBundleVersion()
    {
        // Return the value from the file because the C# API (PlayerSettings.bundleVersion) returns an older value
        // during the GitHub Actions build for whatever reason.
        string[] projectSettingsAssetLines = File.ReadAllLines("ProjectSettings/ProjectSettings.asset");
        string bundleVersionLine = projectSettingsAssetLines.FirstOrDefault(line => line.Contains("bundleVersion:"));
        string bundleVersion = bundleVersionLine.Replace("bundleVersion:", "").Trim();
        Debug.Log($"bundleVersion from ProjectSettings/ProjectSettings.asset: {bundleVersion}");
        return bundleVersion;
    }

    public static string GetBuildOutputFolderFromBuildOptions(CustomBuildOptions options)
    {
        return GetBuildOutputFolder(options.appName, options.buildTarget);
    }

    private static string GetBuildOutputFolder(string appName, BuildTarget buildTarget)
    {
        string buildFolderPath = $"{Application.dataPath}/../../Build/";
        if (buildTarget == BuildTarget.Android)
        {
            return buildFolderPath;
        }

        string versionName = $"v{GetPlayerSettingsFileBundleVersion()}";
        string outputFolderName = $"{ReplaceSpaces(appName)}-{versionName}-{GetTargetPlatformName(buildTarget)}";
        return $"{buildFolderPath}{outputFolderName}";
    }

    private static string ReplaceSpaces(string text)
    {
        return text.Replace(" ", "");
    }

    private static LogType GetLogType(BuildResult summaryResult)
    {
        return summaryResult switch
        {
            BuildResult.Failed => LogType.Error,
            BuildResult.Cancelled => LogType.Warning,
            BuildResult.Unknown => LogType.Warning,
            BuildResult.Succeeded => LogType.Log,
            _ => throw new ArgumentOutOfRangeException(nameof(summaryResult), summaryResult, null)
        };
    }

    private static string GetExecutableName(string appName, BuildTarget buildTarget, bool buildAppBundleForGooglePlay, bool configureKeystoreForAndroidBuild)
    {
        string versionName = $"v{GetPlayerSettingsFileBundleVersion()}";
        string androidFileExtension = buildAppBundleForGooglePlay
            ? $"aab"
            : $"apk";
        string signedPart = configureKeystoreForAndroidBuild
            ? "-signed"
            : "";
        return buildTarget switch
        {
            BuildTarget.StandaloneWindows => $"{appName}.exe",
            BuildTarget.StandaloneWindows64 => $"{appName}.exe",
            BuildTarget.StandaloneLinux64 => $"{appName}",
            BuildTarget.Android => $"{ReplaceSpaces(appName)}-{versionName}-Android{signedPart}.{androidFileExtension}",
            _ => $"",
        };
    }

    private static string GetTargetPlatformName(BuildTarget buildTarget)
    {
        return buildTarget switch
        {
            BuildTarget.StandaloneOSX => $"macOS",
            BuildTarget.StandaloneWindows => $"Windows32",
            BuildTarget.StandaloneWindows64 => $"Windows64",
            BuildTarget.StandaloneLinux64 => $"Linux64",
            BuildTarget.Android => $"Android",
            BuildTarget.iOS => $"iOS",
            _ => buildTarget.ToString()
        };
    }

    private static string[] GetEnabledScenePaths()
    {
        return EditorBuildSettings.scenes
            .Where(scene => scene.enabled && !string.IsNullOrEmpty(scene.path))
            .Select(scene => scene.path)
            .ToArray();
    }

    public static bool TryGetEnvironmentVariable(string key, out string value)
    {
        value = Environment.GetEnvironmentVariable(key);
        return !value.IsNullOrEmpty();
    }

    public static string GetEnvironmentVariableOrThrow(string key)
    {
        if (!TryGetEnvironmentVariable(key, out string value))
        {
            throw new Exception($"{key} environment variable missing");
        }

        return value;
    }

    private static void CompressDirectoryToZipFile(string directoryPath, string outputFilePath, int compressionLevel = 9)
    {
        Debug.Log($"Creating ZIP file {outputFilePath} from {directoryPath}");
        new FastZip().CreateZip(outputFilePath, directoryPath, true, ".*", ".*");
        Debug.Log($"Created ZIP file {outputFilePath} from {directoryPath}");
    }

    public static void PushApkToDevice(string appName)
    {
        string outputFolder = GetBuildOutputFolder(appName, BuildTarget.Android);
        string apkFileName = GetExecutableName(appName, BuildTarget.Android, false, false);
        string apkLocation = outputFolder + $"/{apkFileName}";
        if (apkLocation.IsNullOrEmpty()
            || !File.Exists(apkLocation))
        {
            Debug.LogError($"Did not find apk file '{apkLocation}'. Build the apk first.");
            return;
        }

        string androidSdkRoot = EditorPrefs.GetString("AndroidSdkRoot");
        if (androidSdkRoot.IsNullOrEmpty()
            || !Directory.Exists(androidSdkRoot))
        {
            androidSdkRoot = PlayerPrefs.GetString("AndroidSdkRoot");
            if (androidSdkRoot.IsNullOrEmpty()
                || !Directory.Exists(androidSdkRoot))
            {
                androidSdkRoot = EditorUtility.OpenFolderPanel("Android SDK location", Environment.CurrentDirectory, "");
                if (androidSdkRoot.IsNullOrEmpty()
                    || !Directory.Exists(androidSdkRoot))
                {
                    Debug.LogError($"No Android SDK found");
                    return;
                }
                PlayerPrefs.SetString("AndroidSdkRoot", androidSdkRoot);
            }
        }

        string adbLocation = androidSdkRoot + "/platform-tools/adb";
#if UNITY_EDITOR_WIN
        adbLocation += ".exe";
#endif

        if (adbLocation.IsNullOrEmpty()
            || !File.Exists(adbLocation))
        {
            Debug.LogError($"Did not find adb in {adbLocation}");
            return;
        }

        Debug.Log($"Pushing '{apkLocation}' to device.");
        ProcessStartInfo info = new ProcessStartInfo
        {
            FileName = adbLocation,
            Arguments = $"install -r -d \"{apkLocation}\"",
            WorkingDirectory = Path.GetDirectoryName(adbLocation),
        };
        Process.Start(info);
    }

    public static string GetUnityVersion()
    {
        return Application.unityVersion;
    }
}
