using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

// Fills a file with build information before Unity performs the actual build.
public class BuildInfoGenerator : IPreprocessBuildWithReport
{
    public static readonly string timeStampPropertyName = "build_timestamp";
    public static readonly string versionFile = "Assets/Common/Version/version.properties.txt";

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
        string[] versionFileLines = File.ReadAllLines(versionFile);
        for (int i = 0; i < versionFileLines.Length; i++)
        {
            string line = versionFileLines[i];
            if (line.StartsWith(timeStampPropertyName, true, CultureInfo.InvariantCulture))
            {
                versionFileLines[i] = $"{timeStampPropertyName} = {timeStamp}";
            }
        }
        File.WriteAllLines(versionFile, versionFileLines);
    }
}
