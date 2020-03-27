using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

// Creates a file with build information before Unity performs the actual build.
public class BuildInfoGenerator : IPreprocessBuildWithReport
{
    public int callbackOrder
    {
        get
        {
            return 0;
        }
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        UpdateVersionClass();
    }

    private void UpdateVersionClass()
    {
        string versionClassFile = "Assets/Common/Version.cs";
        string timeStamp = DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        string[] versionClassLines = File.ReadAllLines(versionClassFile);
        for (int i = 0; i < versionClassLines.Length; i++)
        {
            string line = versionClassLines[i];
            if (line.Contains("buildTimeStamp"))
            {
                versionClassLines[i] = Regex.Replace(line,
                    ".* buildTimeStamp = .*",
                    $"    public static readonly string buildTimeStamp = \"{timeStamp}\";");
            }
        }
        File.WriteAllLines(versionClassFile, versionClassLines);
    }
}
