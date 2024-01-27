using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
#if UNITY_IOS
using System.IO;
using UnityEditor.iOS.Xcode;
#endif

/**
 * Add an .entitlements file to the generated XCode project of an iOS build.
 * The .entitlements file is used by Apple to grant capabilities access rights.
 * See https://forum.unity.com/threads/how-to-put-ios-entitlements-file-in-a-unity-project.442277/
 */
public class AddAppleEntitlementsPostProcessBuild : IPostprocessBuildWithReport
{
    // Should be called later in the build, so use higher number.
    public int callbackOrder => 100;

    public void OnPostprocessBuild(BuildReport report)
    {
        BuildTarget buildTarget = report.summary.platform;
        string buildPath = report.summary.outputPath;
        if (buildTarget != BuildTarget.iOS)
        {
            return;
        }

#if UNITY_IOS
        string entitlementsFilePath = $"{Application.dataPath}/Editor/AppleEntitlements/Unity-iPhone.entitlements";
        if (!File.Exists(entitlementsFilePath))
        {
            throw new Exception($"Entitlements file for XCode project not found in '{entitlementsFilePath}'.");
        }

        Debug.Log($"Adding .entitlements file to generated XCode project: {entitlementsFilePath}");

        string projectPath = PBXProject.GetPBXProjectPath(buildPath);
        PBXProject proj = new PBXProject();
        proj.ReadFromFile(projectPath);

        string targetName = "Unity-iPhone";
        string targetGuid = proj.GetUnityMainTargetGuid();
        string sourceFilePath = entitlementsFilePath;
        string fileName = Path.GetFileName(sourceFilePath);
        string targetFilePath = $"{buildPath}/{targetName}/{fileName}";
        FileUtil.CopyFileOrDirectory(sourceFilePath, targetFilePath);
        proj.AddFile($"{targetName}/{fileName}", fileName);
        proj.AddBuildProperty(targetGuid, "CODE_SIGN_ENTITLEMENTS", targetName + "/" + fileName);
        proj.WriteToFile(projectPath);

        Debug.Log($"Created .entitlements file: {targetFilePath}");
#endif
    }
}
