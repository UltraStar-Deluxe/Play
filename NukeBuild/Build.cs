using System;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.Unity;
using static Nuke.Common.Tools.Unity.UnityTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace DefaultNamespace;

class Build : NukeBuild
{
    [Parameter] readonly uint cloneDepth = 1000;
    [Parameter] readonly AbsolutePath buildOutput = RootDirectory / "Builds";

    private readonly AbsolutePath mainGameDir = RootDirectory / "UltraStar Play";
    private readonly AbsolutePath companionAppDir = RootDirectory / "UltraStar Play Companion";

    public static int Main() => Execute<Build>(x => x.BuildMainGameWindows64);

    Target RestoreMainGameDependencies => _ => _
        .DependsOn(RestoreMainGameNuGet)
        .Executes(() =>
        {
            DownloadOneJs();
        });

    Target RestoreMainGameNuGet => _ => _
        .Executes(() =>
        {
            Console.WriteLine("🔄 Running dotnet restore with packages.config...");

            DotNet($"restore \"{GetPackagesConfigFile(mainGameDir)}\" --packages \"{GetPackagesTargetFolder(mainGameDir)}\"");
        });

    Target BuildMainGameWindows64 => _ => _
        .Executes(() =>
        {
            RunUnityBuildMainGame("BuildWindows64");
        });

    Target BuildCompanionAppAndroidApk => _ => _
        .Executes(() =>
        {
            RunUnityBuildCompanionApp("BuildAndroidApk");
        });

    Target BuildAndRunCompanionAppAndroidApk => _ => _
        .Executes(() =>
        {
            RunUnityBuildCompanionApp("BuildAndRunAndroidApk");
        });

    Target BuildCompanionAppSignedAndroidApk => _ => _
        .Executes(() =>
        {
            RunUnityBuildCompanionApp("BuildSignedAndroidApk");
        });

    private AbsolutePath GetPackagesConfigFile(AbsolutePath unityProjectDir)
    {
        return unityProjectDir / "Packages" / "packages.config";
    }

    private AbsolutePath GetPackagesTargetFolder(AbsolutePath unityProjectDir)
    {
        return unityProjectDir / "Packages" / "NuGet";
    }

    private void RunUnityBuildMainGame(string methodName)
    {
        Console.WriteLine("🚀 Building Unity project of main game...");

        Unity(new UnitySettings()
                .SetProjectPath(mainGameDir) // Path to Unity project
                .SetBatchMode(true) // Run in batch mode
                .SetQuit(true) // Quit Unity after build
                .SetExecuteMethod($"MainGameBuildTools.{methodName}") // Specify the build method to execute
                .SetLogFile(buildOutput / "NukeBuildMainGame.log") // Log file for Unity build
        );
    }

    private void RunUnityBuildCompanionApp(string methodName)
    {
        Console.WriteLine("🚀 Building Unity project of companion app...");

        Unity(new UnitySettings()
                .SetProjectPath(companionAppDir)
                .SetBatchMode(true)
                .SetQuit(true)
                .SetExecuteMethod($"CompanionAppBuildTools.{methodName}") // Specify the build method to execute
                .SetLogFile(buildOutput / "NukeBuildCompanionApp.log") // Log file for Unity build
        );
    }

    void DownloadOneJs()
    {
        new GitDownloader
        {
            RemoteUrl = "https://github.com/achimmihca/OneJsRuntimeLoadedStyleSheets.git",
            CommitHash = "8259391b6bbdbd6a445515151bf4aae87e0ba57b",
            TargetDir = mainGameDir / "Assets" / "OneJS",
            Depth = cloneDepth,
            SparseCheckoutPatterns =
            {
                "Assets/OneJS/*",
                "Assets/OneJS.meta"
            },
            MovePostprocess =
            {
                { "Assets/OneJS/*", "." },
                { "Assets/OneJS.meta", "." }
            },
            DeletePostprocess =
            {
                "Assets",
                ".git"
            }
        }.DownloadGitHubDependency();
    }
}
