using System;
using System.IO;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Unity;
using static Nuke.Common.Tools.Unity.UnityTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace DefaultNamespace;

class Build : NukeBuild
{
    [Parameter] readonly uint cloneDepth = 1000;
    [Parameter] readonly AbsolutePath buildOutput = RootDirectory / "Build";
    [Parameter] readonly AbsolutePath unityExecutable = "C:/Program Files/Unity/Hub/Editor/2023.2.12f1/Editor/Unity.exe";

    private readonly AbsolutePath mainGameDir = RootDirectory / "UltraStar Play";
    private readonly AbsolutePath companionAppDir = RootDirectory / "UltraStar Play Companion";

    public static int Main() => Execute<Build>(x => x.BuildMainGameWindows64);

    Target RunMainGameTests => _ => _
        .Executes(() =>
        {
            RunUnityTests(mainGameDir, UnityTestPlatform.EditMode);
            RunUnityTests(mainGameDir, UnityTestPlatform.PlayMode);
        });

    Target RunCompanionAppTests => _ => _
        .Executes(() =>
        {
            RunUnityTests(companionAppDir, UnityTestPlatform.EditMode);
            RunUnityTests(companionAppDir, UnityTestPlatform.PlayMode);
        });

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

    void RunUnityBuildMainGame(string methodName)
    {
        RunUnityBuild(mainGameDir, $"MainGameBuildTools.{methodName}");
    }

    void RunUnityBuildCompanionApp(string methodName)
    {
        RunUnityBuild(companionAppDir, $"CompanionAppBuildTools.{methodName}");
    }

    private void RunUnityTests(AbsolutePath unityProjectDir, UnityTestPlatform unityTestPlatform)
    {
        Console.WriteLine($"🚀 Running Unity tests of '{unityProjectDir.Name}'...");

        UnityRunTests(s => s
            .SetProcessToolPath(unityExecutable)
            .SetProjectPath(unityProjectDir)
            .SetBatchMode(true)
            .SetQuit(true)
            .SetTestPlatform(unityTestPlatform)
            .SetLogFile(buildOutput / $"NukeRunTests-{unityTestPlatform}.log")
        );
    }

    private void RunUnityBuild(AbsolutePath unityProjectDir, string executeMethod)
    {
        Console.WriteLine($"🚀 Building Unity project '{unityProjectDir.Name}' ...");

        Unity(new UnitySettings()
                .SetProjectPath(companionAppDir)
                .SetBatchMode(true)
                .SetQuit(true)
                .SetExecuteMethod(executeMethod)
                .SetLogFile(buildOutput / "NukeBuildCompanionApp.log")
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
