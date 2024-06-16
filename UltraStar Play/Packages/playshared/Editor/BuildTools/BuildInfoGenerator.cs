using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Truncon.Collections;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Fills a file with build information before Unity performs the actual build.
public class BuildInfoGenerator : IPreprocessBuildWithReport
{
    public static readonly string versionPropertyName = "release";
    public static readonly string timeStampPropertyName = "build_timestamp";
    public static readonly string commitShortHashPropertyName = "commit_hash";
    public static readonly string unityVersionPropertyName = "unity_version";
    public static readonly string websiteLinkPropertyName = "website_link";
    public static readonly string versionFile = "Assets/VERSION.txt";

    private static readonly string websiteLinkPropertyValue = "https://melodymania.org/get";

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        UpdateVersionFile();
    }

    private static void UpdateVersionFile()
    {
        string bundleVersion = BuildUtils.GetPlayerSettingsFileBundleVersion();
        string timeStamp = DateTime.Now.ToString("yyMMddHHmm", CultureInfo.InvariantCulture);
        string commitShortHash = GitUtils.GetCurrentCommitShortHash();
        string unityVersion = BuildUtils.GetUnityVersion();

        IDictionary<string, string> propertyNameToValueMap = new InsertionOrderedDictionary<string, string>();
        propertyNameToValueMap.Add(versionPropertyName, bundleVersion);
        propertyNameToValueMap.Add(commitShortHashPropertyName, commitShortHash);
        propertyNameToValueMap.Add(unityVersionPropertyName, unityVersion);
        propertyNameToValueMap.Add(timeStampPropertyName, timeStamp);
        propertyNameToValueMap.Add(websiteLinkPropertyName, websiteLinkPropertyValue);

        Debug.Log($"Updating {versionFile} with {JsonConverter.ToJson(propertyNameToValueMap)}");
        List<string> versionFileLines = propertyNameToValueMap
            .Select(entry => $"{entry.Key} = {entry.Value}")
            .ToList();
        File.WriteAllLines(versionFile, versionFileLines);

        Debug.Log($"New contents of {versionFile}:\n{versionFileLines.JoinWith(", ")}");

        // Unity needs a hint that this asset has changed.
        AssetDatabase.ImportAsset(versionFile);
    }
}
