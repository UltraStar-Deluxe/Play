using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Fills a file with build information before Unity performs the actual build.
public class BuildInfoGenerator : IPreprocessBuildWithReport
{
    private static readonly string versionPropertyName = "release";
    private static readonly string timeStampPropertyName = "build_timestamp";
    private static readonly string commitShortHashPropertyName = "commit_hash";
    private static readonly string versionFile = "Assets/VERSION.txt";

    public int callbackOrder
    {
        get
        {
            return 0;
        }
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        UpdateVersionFile();
    }

    private static void UpdateVersionFile()
    {
        string bundleVersion = GetPlayerSettingsFileBundleVersion();
        string timeStamp = DateTime.Now.ToString("yyMMddHHmm", CultureInfo.InvariantCulture);
        string commitShortHash = GitUtils.GetCurrentCommitShortHash();

        Dictionary<string, string> propertyNameToValueMap = new();
        propertyNameToValueMap.Add(versionPropertyName, bundleVersion);
        propertyNameToValueMap.Add(timeStampPropertyName, timeStamp);
        propertyNameToValueMap.Add(commitShortHashPropertyName, commitShortHash);

        Debug.Log($"Updating {versionFile} with {JsonConverter.ToJson(propertyNameToValueMap)}");

        string[] versionFileLines = File.ReadAllLines(versionFile);
        for (int i = 0; i < versionFileLines.Length; i++)
        {
            string line = versionFileLines[i];
            propertyNameToValueMap.ForEach(entry =>
            {
                string propertyName = entry.Key;
                string propertyValue = entry.Value;
                if (line.StartsWith(entry.Key, true, CultureInfo.InvariantCulture))
                {
                    versionFileLines[i] = $"{propertyName} = {propertyValue}";
                }
            });
        }
        File.WriteAllLines(versionFile, versionFileLines);

        Debug.Log($"New contents of {versionFile}:\n{versionFileLines.ToCsv(", ")}");

        // Unity needs a hint that this asset has changed.
        AssetDatabase.ImportAsset(versionFile);
    }

    private static string GetPlayerSettingsFileBundleVersion()
    {
        // Return the value from the file because the C# API (PlayerSettings.bundleVersion) returns an older value
        // during the GitHub Actions build for whatever reason.
        string[] projectSettingsAssetLines = File.ReadAllLines("ProjectSettings/ProjectSettings.asset");
        string bundleVersionLine = projectSettingsAssetLines.FirstOrDefault(line => line.Contains("bundleVersion:"));
        string bundleVersion = bundleVersionLine.Replace("bundleVersion:", "").Trim();
        Debug.Log($"bundleVersion from ProjectSettings/ProjectSettings.asset: {bundleVersion}");
        return bundleVersion;
    }
}
