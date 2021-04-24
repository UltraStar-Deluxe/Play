using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

// Fills a file with build information before Unity performs the actual build.
public class BuildInfoGenerator : IPreprocessBuildWithReport
{
    public static readonly string versionPropertyName = "release";
    public static readonly string timeStampPropertyName = "build_timestamp";
    public static readonly string commitShortHashPropertyName = "commit_hash";
    public static readonly string versionFile = "Assets/VERSION.txt";

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

    private void UpdateVersionFile()
    {
        string timeStamp = DateTime.Now.ToString("yyMMddHHmm", CultureInfo.InvariantCulture);
        string commitShortHash = GitUtils.GetCurrentCommitShortHash();
        
        string[] versionFileLines = File.ReadAllLines(versionFile);
        for (int i = 0; i < versionFileLines.Length; i++)
        {
            string line = versionFileLines[i];
            if (line.StartsWith(versionPropertyName, true, CultureInfo.InvariantCulture))
            {
                versionFileLines[i] = $"{versionPropertyName} = {PlayerSettings.bundleVersion}";
            }
            
            if (line.StartsWith(timeStampPropertyName, true, CultureInfo.InvariantCulture))
            {
                versionFileLines[i] = $"{timeStampPropertyName} = {timeStamp}";
            }
            
            if (line.StartsWith(commitShortHashPropertyName, true, CultureInfo.InvariantCulture))
            {
                versionFileLines[i] = $"{commitShortHashPropertyName} = {commitShortHash}";
            }
        }
        File.WriteAllLines(versionFile, versionFileLines);
        // Unity needs a hint that this asset has changed.
        AssetDatabase.ImportAsset(versionFile);
    }
}
