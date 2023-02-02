using UnityEditor;
using UnityEngine;

public static class UltraStarPlayBuildTools
{
    private static readonly string appName = "UltraStar Play";

    [MenuItem("Tools/Build/Build for release (Windows, macOS, Linux, signed apk, signed app bundle)")]
    public static void BuildAllForRelease()
    {
        BuildWindows64();
        BuildMacOS();
        BuildLinux64();
        BuildSignedAndroidApk();
        BuildSignedAndroidAppBundle();
        // BuildIOS();
    }

    [MenuItem("Tools/Build/Windows64")]
    public static void BuildWindows64()
    {
        BuildUtils.PerformCustomBuild(CreateCustomBuildOptions(BuildTarget.StandaloneWindows64));
    }

    [MenuItem("Tools/Build/Linux64")]
    public static void BuildLinux64()
    {
        BuildUtils.PerformCustomBuild(CreateCustomBuildOptions(BuildTarget.StandaloneLinux64));
    }

    [MenuItem("Tools/Build/macOS")]
    public static void BuildMacOS()
    {
        BuildUtils.PerformCustomBuild(CreateCustomBuildOptions(BuildTarget.StandaloneOSX));
    }

    [MenuItem("Tools/Build/Android - Push apk to device")]
    public static void PushToAndroid()
    {
        BuildUtils.PushApkToDevice(appName);
    }

    [MenuItem("Tools/Build/Android - Build apk")]
    public static void BuildAndroidApk()
    {
        BuildUtils.PerformCustomBuild(CreateCustomBuildOptions(BuildTarget.Android));
    }

    [MenuItem("Tools/Build/Android - Build and run apk")]
    public static void BuildAndRunAndroidApk()
    {
        CustomBuildOptions customBuildOptions = CreateCustomBuildOptions(BuildTarget.Android);
        customBuildOptions.buildOptions |= BuildOptions.AutoRunPlayer;
        BuildUtils.PerformCustomBuild(customBuildOptions);
    }

    [MenuItem("Tools/Build/Android - Build signed apk")]
    public static void BuildSignedAndroidApk()
    {
        CustomBuildOptions customBuildOptions = CreateCustomBuildOptions(BuildTarget.Android);
        customBuildOptions.configureKeystoreForAndroidBuild = true;
        BuildUtils.PerformCustomBuild(customBuildOptions);
    }

    [MenuItem("Tools/Build/Android - Build and run signed apk")]
    public static void BuildAndRunSignedAndroidApk()
    {
        CustomBuildOptions customBuildOptions = CreateCustomBuildOptions(BuildTarget.Android);
        customBuildOptions.buildOptions |= BuildOptions.AutoRunPlayer;
        customBuildOptions.configureKeystoreForAndroidBuild = true;
        BuildUtils.PerformCustomBuild(customBuildOptions);
    }

    [MenuItem("Tools/Build/Android - Build signed app bundle")]
    public static void BuildSignedAndroidAppBundle()
    {
        CustomBuildOptions customBuildOptions = CreateCustomBuildOptions(BuildTarget.Android);
        customBuildOptions.configureKeystoreForAndroidBuild = true;
        customBuildOptions.buildAppBundleForGooglePlay = true;
        BuildUtils.PerformCustomBuild(customBuildOptions);
    }

    [MenuItem("Tools/Build/Android - Build and run signed app bundle")]
    public static void BuildAndRunSignedAndroidAppBundle()
    {
        CustomBuildOptions customBuildOptions = CreateCustomBuildOptions(BuildTarget.Android);
        customBuildOptions.buildOptions |= BuildOptions.AutoRunPlayer;
        customBuildOptions.configureKeystoreForAndroidBuild = true;
        customBuildOptions.buildAppBundleForGooglePlay = true;
        BuildUtils.PerformCustomBuild(customBuildOptions);
    }

    [MenuItem("Tools/Build/iOS")]
    public static void BuildIOS()
    {
        BuildUtils.PerformCustomBuild(CreateCustomBuildOptions(BuildTarget.iOS));
    }

    private static CustomBuildOptions CreateCustomBuildOptions(BuildTarget buildTarget)
    {
        CustomBuildOptions customBuildOptions = new CustomBuildOptions(appName, buildTarget);
        customBuildOptions.compressOutputFolderToZipFile = true;
        return customBuildOptions;
    }
}
