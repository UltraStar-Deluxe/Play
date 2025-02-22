using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class MobileBuildUtils
{
    private const string KeystorePathEnvironmentVariable = "UNITY_KEYSTORE_PATH";
    private const string KeystorePasswordEnvironmentVariable = "UNITY_KEYSTORE_PASSWORD";
    private const string KeystoreKeyAliasEnvironmentVariable = "UNITY_KEYSTORE_KEY_ALIAS";
    private const string KeystoreKeyAliasPasswordEnvironmentVariable = "UNITY_KEYSTORE_KEY_ALIAS_PASSWORD";

    private static string IgnoredAssetsOfMobileBuildFolder => "IgnoredAssetsOfMobileBuild";

    private static readonly List<string> ignoredFoldersOfMobileBuild = new()
    {
        "Assets/StreamingAssets/SpleeterMsvcExe",
        "Assets/StreamingAssets/BasicPitchExe",
        "Assets/StreamingAssets/SpeechRecognitionModels",
    };

    public static bool IsMobileBuild(BuildTarget optionsBuildTarget)
    {
        return optionsBuildTarget is BuildTarget.Android or BuildTarget.iOS;
    }

    public static void ConfigureAndroidBuildSettings(CustomBuildOptions options)
    {
        PlayerSettings.Android.bundleVersionCode = BuildUtils.GetBundleVersionFromCurrentTime();

        // Build Android app bundle (aab file) or apk file
        EditorUserBuildSettings.buildAppBundle = options.buildAppBundleForGooglePlay;

        if (options.buildAppBundleForGooglePlay)
        {
            // Build the app bundle also for 64bit CPU architectures.
            // Otherwise it cannot be uploaded to Google Play.
            // Note that this build takes considerably more time.
            // Must set the scripting backend to IL2CPP to build for non-ARMv7 architectures.
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7
                                                         | AndroidArchitecture.ARM64;
        }
        else
        {
            // Build the app only for ARMv7 using Mono scripting backend.
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.Mono2x);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
        }

        if (options.configureKeystoreForAndroidBuild)
        {
            ConfigureKeystoreForAndroidBuild();
        }
        else
        {
            PlayerSettings.Android.useCustomKeystore = false;
        }
    }

    public static void ConfigureIosBuildSettings(CustomBuildOptions options)
    {
        PlayerSettings.iOS.buildNumber = BuildUtils.GetBundleVersionFromCurrentTime().ToString();
    }


    private static void ConfigureKeystoreForAndroidBuild()
    {
        if (!BuildUtils.TryGetEnvironmentVariable(KeystorePathEnvironmentVariable, out string keystorePath))
        {
            throw new Exception($"Environment variable {KeystorePathEnvironmentVariable} not found");
        }

        if (!File.Exists(keystorePath))
        {
            throw new Exception($"Keystore not found in {keystorePath}");
        }

        if (!BuildUtils.TryGetEnvironmentVariable(KeystorePasswordEnvironmentVariable, out string keystorePassword))
        {
            throw new Exception($"Environment variable ${KeystorePasswordEnvironmentVariable}");
        }

        if (!BuildUtils.TryGetEnvironmentVariable(KeystoreKeyAliasEnvironmentVariable, out string aliasName))
        {
            throw new Exception($"$Environment variable {KeystoreKeyAliasEnvironmentVariable} not set");
        }

        if (!BuildUtils.TryGetEnvironmentVariable(KeystoreKeyAliasPasswordEnvironmentVariable, out string aliasPassword))
        {
            throw new Exception($"Environment variable ${KeystoreKeyAliasPasswordEnvironmentVariable}");
        }

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = keystorePath;
        PlayerSettings.Android.keystorePass = keystorePassword;
        PlayerSettings.Android.keyaliasName = aliasName;
        PlayerSettings.Android.keyaliasPass = aliasPassword;
    }


    public static void ExcludeAssetsBeforeMobileBuild()
    {
        DirectoryUtils.CreateDirectory(IgnoredAssetsOfMobileBuildFolder);
        foreach (string path in ignoredFoldersOfMobileBuild)
        {
            string src = path;
            string dest = $"{IgnoredAssetsOfMobileBuildFolder}/{path}";
            Debug.Log($"Exclude directory before mobile build: {src} -> {dest}");
            if (!DirectoryUtils.Exists(src))
            {
                Debug.LogWarning("Cannot move directory. Directory does not exist: " + src);
                continue;
            }

            DirectoryUtils.CreateDirectory(new DirectoryInfo(dest).Parent.FullName);
            Directory.Move(src, dest);
            try
            {
                FileUtils.MoveFileOverwriteIfExists(src + ".meta", dest + ".meta");
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to move .meta file: " + e.Message);
            }

            // Wait for file operation to complete
            Thread.Sleep(100);

            // Delete empty directory if needed.
            if (Directory.Exists(src)
                && Directory.GetFiles(src).IsNullOrEmpty())
            {
                Directory.Delete(src);
            }
        }
    }

    public static void IncludeAssetsAfterMobileBuild()
    {
        DirectoryUtils.CreateDirectory(IgnoredAssetsOfMobileBuildFolder);
        foreach (string path in ignoredFoldersOfMobileBuild)
        {
            string src = $"{IgnoredAssetsOfMobileBuildFolder}/{path}";
            string dest = path;
            Debug.Log($"Include directory after mobile build: {src} -> {dest}");
            if (!DirectoryUtils.Exists(src))
            {
                Debug.LogWarning("Cannot move directory. Directory does not exist: " + src);
                continue;
            }

            DirectoryUtils.CreateDirectory(new DirectoryInfo(dest).Parent.FullName);
            Directory.Move(src, dest);
            try
            {
                FileUtils.MoveFileOverwriteIfExists(src + ".meta", dest + ".meta");
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to move .meta file: " + e.Message);
            }

            // Delete empty directory if needed.
            if (Directory.Exists(src)
                && Directory.GetFiles(src).IsNullOrEmpty())
            {
                Directory.Delete(src);
            }
        }
    }
}
