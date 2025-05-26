using UnityEditor;

public static class CompanionAppBuildTools
{
    private static readonly string appName = "UltraStar Play Companion";

    [MenuItem("Tools/Build/Android - Build apk")]
    public static void BuildAndroidApk()
    {
        BuildUtils.PerformCustomBuild(CreateCustomBuildOptions(BuildTarget.Android));
    }

    [MenuItem("Tools/Build/Android - Build and run apk")]
    public static void BuildAndRunAndroidApk()
    {
        CustomBuildOptions customBuildOptions = CreateCustomBuildOptions(BuildTarget.Android);
        customBuildOptions.buildOptions = BuildOptions.AutoRunPlayer;
        BuildUtils.PerformCustomBuild(customBuildOptions);
    }

    [MenuItem("Tools/Build/Android - Build signed app bundle (aab)")]
    public static void BuildSignedAndroidAppBundle()
    {
        CustomBuildOptions customBuildOptions = CreateCustomBuildOptions(BuildTarget.Android);
        customBuildOptions.configureKeystoreForAndroidBuild = true;
        customBuildOptions.buildAppBundleForGooglePlay = true;
        BuildUtils.PerformCustomBuild(customBuildOptions);
    }

    [MenuItem("Tools/Build/Android - Build and run signed app bundle (aab)")]
    public static void BuildAndRunSignedAndroidAppBundle()
    {
        CustomBuildOptions customBuildOptions = CreateCustomBuildOptions(BuildTarget.Android);
        customBuildOptions.buildOptions = BuildOptions.AutoRunPlayer;
        customBuildOptions.configureKeystoreForAndroidBuild = true;
        customBuildOptions.buildAppBundleForGooglePlay = true;
        BuildUtils.PerformCustomBuild(customBuildOptions);
    }

    [MenuItem("Tools/Build/Android - Push apk to device")]
    public static void PushToAndroid()
    {
        BuildUtils.PushApkToDevice(appName);
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
