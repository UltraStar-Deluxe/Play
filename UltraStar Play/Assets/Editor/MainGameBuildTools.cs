using UnityEditor;

public static class MainGameBuildTools
{
    private static readonly string appName = "Melody Mania";

    [MenuItem("Tools/Build/Windows64")]
    public static void BuildWindows64()
    {
        CustomBuildOptions customBuildOptions = CreateCustomBuildOptions(BuildTarget.StandaloneWindows64);
        BuildUtils.PerformCustomBuild(customBuildOptions);
    }

    [MenuItem("Tools/Build/Windows64 (dev build)")]
    public static void BuildWindows64ForDev()
    {
        CustomBuildOptions customBuildOptions = CreateCustomBuildOptions(BuildTarget.StandaloneWindows64);
        customBuildOptions.buildOptions |= BuildOptions.Development;
        BuildUtils.PerformCustomBuild(customBuildOptions);
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
        // customBuildOptions.compressOutputFolderToZipFile = true;
        return customBuildOptions;
    }
}
