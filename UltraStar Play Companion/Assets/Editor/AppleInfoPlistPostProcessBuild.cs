using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
using UnityEngine;

/**
 * Adds some properties to Info.plist file of the generated XCode project of an iOS build.
 * Some properties such as MicrophoneUsageDescription can be set from Unity's Player Settings.
 * Others properties like NSLocalNetworkUsageDescription are added here.
 */
public class IosInfoPlistPostProcessBuild : IPostprocessBuildWithReport
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
        // Load XCode project
        string projectPath = PBXProject.GetPBXProjectPath(buildPath);
        PBXProject proj = new PBXProject();
        proj.ReadFromFile(projectPath);

        // Add properties to Info.plist file
        string plistPath = $"{buildPath}/Info.plist";
        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        PlistElementDict rootDict = plist.root;
        rootDict.SetString("NSLocalNetworkUsageDescription",
            "This app uses your local network to find and communicate with the game. " +
            "You will not be able to connect to the game without this permission.");
        plist.WriteToFile(plistPath);

        // Write XCode project to disk
        proj.WriteToFile(projectPath);

        Debug.Log($"Updated '{plistPath}' with NSLocalNetworkUsageDescription");
#endif
    }
}
