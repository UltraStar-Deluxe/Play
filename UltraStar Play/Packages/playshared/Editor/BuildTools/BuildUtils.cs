using System;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class BuildUtils
{
    private const string KeystorePathEnvironmentVariable = "UNITY_KEYSTORE_PATH";
    private const string KeystorePasswordEnvironmentVariable = "UNITY_KEYSTORE_PASSWORD";
    private const string KeystoreKeyAliasEnvironmentVariable = "UNITY_KEYSTORE_KEY_ALIAS";
    private const string KeystoreKeyAliasPasswordEnvironmentVariable = "UNITY_KEYSTORE_KEY_ALIAS_PASSWORD";

    public static void PerformCustomBuild(CustomBuildOptions options)
    {
        string executableName = GetExecutableName(options.appName, options.buildTarget, options.buildAppBundleForGooglePlay, options.configureKeystoreForAndroidBuild);
        string outputFolderPath = GetBuildOutputFolder(options.appName, options.buildTarget);
        string executableFileInOutputFolder = !executableName.IsNullOrEmpty() ? $"/{executableName}" : "";
        string fullOutputPath = $"{outputFolderPath}{executableFileInOutputFolder}";
        string[] enabledScenePaths = GetEnabledScenePaths();
        Debug.Log($"Starting build of {options.appName} for {options.buildTarget}. Build options: {options.buildOptions}. Target path: {Path.GetFullPath(fullOutputPath)}");

        if (options.configureKeystoreForAndroidBuild)
        {
            ConfigureKeystoreForAndroidBuild();
        }
        else
        {
            PlayerSettings.Android.useCustomKeystore = false;
        }

        BuildReport buildReport = BuildPipeline.BuildPlayer(enabledScenePaths, fullOutputPath, options.buildTarget, options.buildOptions);

        LogType logType = GetLogType(buildReport.summary.result);
        TimeSpan buildDuration = buildReport.summary.buildEndedAt - buildReport.summary.buildStartedAt;
        Debug.unityLogger.Log(logType, $"Built {options.appName} for {options.buildTarget}. Build options: {options.buildOptions}. Target path: {Path.GetFullPath(fullOutputPath)}. Duration: {buildDuration.TotalSeconds} seconds");

        if (options.compressOutputFolderToZipFile
            && options.buildTarget
                is BuildTarget.StandaloneOSX
                or BuildTarget.StandaloneLinux64
                or BuildTarget.StandaloneWindows64
                or BuildTarget.StandaloneWindows)
        {
            string generatedFolderPath = options.buildTarget == BuildTarget.StandaloneOSX
                ? $"{outputFolderPath}.app"
                : outputFolderPath;
            CompressDirectoryToZipFile(generatedFolderPath, outputFolderPath + ".zip");
        }
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

    private static string GetBuildOutputFolder(string appName, BuildTarget buildTarget)
    {
        string buildFolderPath = $"{Application.dataPath}../../../Build/";
        if (buildTarget == BuildTarget.Android)
        {
            return buildFolderPath;
        }

        string versionName = $"v{GetPlayerSettingsFileBundleVersion()}";
        string outputFolderName = $"{ReplaceSpaces(appName)}-{versionName}-{GetTargetPlatformName(buildTarget)}";
        return $"{buildFolderPath}/{outputFolderName}";
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

    private static bool TryGetEnvironmentVariable(string key, out string value)
    {
        value = Environment.GetEnvironmentVariable(key);
        return !value.IsNullOrEmpty();
    }

    private static void ConfigureKeystoreForAndroidBuild()
    {
        if (!TryGetEnvironmentVariable(KeystorePathEnvironmentVariable, out string keystorePath))
        {
            throw new Exception($"Environment variable {KeystorePathEnvironmentVariable} not found");
        }

        if (!File.Exists(keystorePath))
        {
            throw new Exception($"Keystore not found in {keystorePath}");
        }

        if (!TryGetEnvironmentVariable(KeystorePasswordEnvironmentVariable, out string keystorePassword))
        {
            throw new Exception($"Environment variable ${KeystorePasswordEnvironmentVariable}");
        }

        if (!TryGetEnvironmentVariable(KeystoreKeyAliasEnvironmentVariable, out string aliasName))
        {
            throw new Exception($"$Environment variable {KeystoreKeyAliasEnvironmentVariable} not set");
        }

        if (!TryGetEnvironmentVariable(KeystoreKeyAliasPasswordEnvironmentVariable, out string aliasPassword))
        {
            throw new Exception($"Environment variable ${KeystoreKeyAliasPasswordEnvironmentVariable}");
        }

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = keystorePath;
        PlayerSettings.Android.keystorePass = keystorePassword;
        PlayerSettings.Android.keyaliasName = aliasName;
        PlayerSettings.Android.keyaliasPass = aliasPassword;
    }

    private static void CompressDirectoryToZipFile(string directoryPath, string outputFilePath, int compressionLevel = 9)
    {
        Debug.Log($"Creating ZIP file {outputFilePath} from {directoryPath}");
        new FastZip().CreateZip(outputFilePath, directoryPath, true, ".*", ".*");
        Debug.Log($"Created ZIP file {outputFilePath} from {directoryPath}");
    }
}
