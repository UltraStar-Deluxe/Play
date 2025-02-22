using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Serilog.Events;
using UnityEditor;
using UnityEngine;

public class SteamBuildUtils
{
    private const string SteamUsernameEnvironmentVariable = "STEAM_USERNAME";
    private const string SteamPasswordEnvironmentVariable = "STEAM_PASSWORD";

    private static readonly List<string> undesiredPathsForSteamUpload = new()
    {
        "Melody Mania_Data/StreamingAssets/Mods/usdb.animux.de-SongRepository",
        "Melody Mania_Data/StreamingAssets/Mods/CoverAndBackgroundImageFromMusicBrainz",
        "Melody Mania_Data/StreamingAssets/Mods/UseYouTubeVideoIdInTxtFiles",
    };

    public static void UploadBuildOutputToSteam(CustomBuildOptions options)
    {
        if (!(options.buildTarget
                is BuildTarget.StandaloneWindows
                or BuildTarget.StandaloneWindows64
                or BuildTarget.StandaloneLinux64
                or BuildTarget.StandaloneOSX))
        {
            throw new Exception($"Cannot upload to Steam with build target {options.buildTarget}");
        }

        // Get path to latest build
        string outputFolderPath = BuildUtils.GetBuildOutputFolderFromBuildOptions(options);

        DeleteFilesAndFoldersThatShouldNotBeUploadedToSteam(outputFolderPath);

        bool shouldUpload = EditorUtility.DisplayDialog(
            "Upload to Steam",
            "Upload latest build result to Steam?",
            "Yes",
            "No");
        if (!shouldUpload)
        {
            Debug.Log("Canceled upload to Steam");
            return;
        }
        Debug.Log("Uploading build to Steam...");

        // Get app version
        string bundleVersion = BuildUtils.GetPlayerSettingsFileBundleVersion();
        string timeStamp = DateTime.Now.ToString("yyMMddHHmm", CultureInfo.InvariantCulture);
        string commitShortHash = GitUtils.GetCurrentCommitShortHash();

        // Prepare configuration
        string sourceFolder = outputFolderPath;
        string steamcmdExecutablePath = GetSteamcmdExecutablePath();
        string vdfFilePath = GetSteamVdfFilePath();
        string destinationFolder = GetSteamUploadContentRootFolder(vdfFilePath);

        // Get secrets from environment variables
        string steamUsername = BuildUtils.GetEnvironmentVariableOrThrow(SteamUsernameEnvironmentVariable);
        string steamPassword = BuildUtils.GetEnvironmentVariableOrThrow(SteamPasswordEnvironmentVariable);

        // Update Steam VDF file description to include the new version
        string vdfFileContent = File.ReadAllText(vdfFilePath);
        // Update desc filed
        string appNameNoSpaces = options.appName.Replace(" ", "_");
        string description = $"{appNameNoSpaces}-{bundleVersion}-{commitShortHash}-{timeStamp}";
        vdfFileContent = Regex.Replace(vdfFileContent, "\"desc\" \"[^\"]*\"", $"\"desc\" \"{description}\"");
        File.WriteAllText(vdfFilePath, vdfFileContent);
        Debug.Log($"Updated VDF file {vdfFilePath} with content:\n{vdfFileContent}");

        // Remove BurstDebugInformation folder from build output path.
        foreach (DirectoryInfo directoryInfo in new DirectoryInfo(outputFolderPath).GetDirectories())
        {
            if (directoryInfo.Name.Contains("BurstDebugInformation") || directoryInfo.Name.Contains("DoNotShip"))
            {
                Debug.Log($"Removing debug folder from build output path: {directoryInfo.FullName}");
                DirectoryUtils.Delete(directoryInfo.FullName, true);
            }
        }

        // Copy build output to target folder.
        DirectoryUtils.CopyAll(sourceFolder, destinationFolder);
        Debug.Log("Copied Unity build output to Steam content path.");

        // Run Steam upload tool
        string steamGuardCode = EditorInputDialog.Show(
            "Steam Guard",
            "Enter Steam Guard Code",
            "");
        if (steamGuardCode.IsNullOrEmpty())
        {
            Debug.Log("No Steam Guard Code provided. Cannot login to Steam.");
            return;
        }

        if (!ProcessUtils.RunProcess(steamcmdExecutablePath,
                $"+login \"{steamUsername}\" \"{steamPassword}\" \"{steamGuardCode}\" +run_app_build \"{vdfFilePath}\" +quit",
                out string steamcmdOutput,
                out string steamcmdErrorOutput,
                LogEventLevel.Information,
                LogEventLevel.Error)
            || !steamcmdErrorOutput.IsNullOrEmpty())
        {
            throw new Exception("Upload to Steam failed.\n" + steamcmdErrorOutput);
        }
        Debug.Log("Uploaded build to Steam successfully.");
    }

    private static string GetSteamcmdExecutablePath()
    {
        return new FileInfo($"{Application.dataPath}/../../tools/steamworks_sdk_157/sdk/tools/ContentBuilder/builder/steamcmd.exe").FullName;
    }

    /**
     * The VDF file that is used to determine which application should be uploaded.
     * Inside this file, the app's content is referenced by separate Depot VDF files.
     */
    private static string GetSteamVdfFilePath()
    {
        return new FileInfo($"{Application.dataPath}/../../tools/steamworks_sdk_157/vdf_files/app_2394070-private-beta.vdf").FullName;
    }

    private static string GetSteamUploadContentRootFolder(string appVdfFilePath)
    {
        string appVdfFileContent = File.ReadAllText(appVdfFilePath);

        // Search for Depot VDF file path
        Regex vdfFilePathRegex = new Regex(@"""(?<path>[^""]+depot_\w+\.vdf)""");
        MatchCollection vdfFilePathMatches = vdfFilePathRegex.Matches(appVdfFileContent);
        if (vdfFilePathMatches.Count != 1)
        {
            throw new IllegalStateException($"Unable to determine VDF file of depot in '{appVdfFilePath}'");
        }
        string depotVdfFile = vdfFilePathMatches.FirstOrDefault().Groups["path"].Value;

        // Search for "contentroot" in Depot VDF file
        string depotVdfFileContent = File.ReadAllText(depotVdfFile);
        Regex contentRootFolderRegex = new Regex(@"""contentroot""\s*""(?<path>[^""]+)""");
        MatchCollection contentRootFolderMatches = contentRootFolderRegex.Matches(depotVdfFileContent);
        if (contentRootFolderMatches.Count != 1)
        {
            throw new IllegalStateException($"Unable to determine contentroot inf VDF file of depot in '{depotVdfFile}'");
        }

        return contentRootFolderMatches.FirstOrDefault().Groups["path"].Value;
    }

    private static void DeleteFilesAndFoldersThatShouldNotBeUploadedToSteam(string outputFolderPath)
    {
        List<string> undesiredPaths = undesiredPathsForSteamUpload
            .Where(path =>
            {
                string pathInBuildOutput = $"{outputFolderPath}/{path}";
                return DirectoryUtils.Exists(path)
                       || FileUtils.Exists(path)
                       || DirectoryUtils.Exists(pathInBuildOutput)
                       || FileUtils.Exists(pathInBuildOutput);
            })
            .ToList();
        if (!undesiredPaths.IsNullOrEmpty())
        {
            bool shouldDelete = EditorUtility.DisplayDialog(
                "Undesired files and folders for Steam upload",
                $"Delete the following undesired files and folders:\n    " +
                $"{undesiredPaths.JoinWith("\n    ")}?",
                "Yes",
                "No");
            if (!shouldDelete)
            {
                throw new Exception("Aborted upload to Steam because undesired files or folder are present.");
            }

            foreach (string undesiredPath in undesiredPaths)
            {
                string fullPath = $"{outputFolderPath}/{undesiredPath}";
                if (DirectoryUtils.Exists(fullPath))
                {
                    Debug.Log($"Deleting folder '{fullPath}'");
                    DirectoryUtils.Delete(fullPath, true);
                }

                if (FileUtils.Exists(fullPath))
                {
                    Debug.Log($"Deleting file '{fullPath}'");
                    FileUtils.Delete(undesiredPath);
                }
            }

            // Re-check to ensure that no more undesired files and folders are present
            DeleteFilesAndFoldersThatShouldNotBeUploadedToSteam(outputFolderPath);
        }
    }

}
