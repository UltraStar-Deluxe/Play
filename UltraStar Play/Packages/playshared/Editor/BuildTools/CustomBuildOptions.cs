using UnityEditor;

public class CustomBuildOptions
{
    public string appName;
    public BuildTarget buildTarget;
    public BuildOptions buildOptions;
    public bool buildAppBundleForGooglePlay;
    public bool configureKeystoreForAndroidBuild;
    public bool compressOutputFolderToZipFile;

    public CustomBuildOptions()
    {
    }

    public CustomBuildOptions(string appName, BuildTarget buildTarget)
    {
        this.appName = appName;
        this.buildTarget = buildTarget;
    }
}
